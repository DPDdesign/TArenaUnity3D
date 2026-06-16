(function (ns) {
  "use strict";

  function makeOffer(id, category, title, detail, cost, operation) {
    return {
      id: id,
      category: category,
      title: title,
      detail: detail,
      cost: cost,
      operation: operation,
      purchased: false,
      resultSource: "offline-local-shop-resolver"
    };
  }

  function firstLossStack(army) {
    return ns.State.liveArmy(army).filter(function (stack) {
      return (stack.lost || 0) > 0;
    }).sort(function (a, b) {
      return (b.lost || 0) - (a.lost || 0);
    })[0] || null;
  }

  function buildShopVisit(run, node) {
    var offers = [];
    var lost = firstLossStack(run.army);
    var skillReward = ns.SkillRuntime.findLegalSkillReward(run.army);
    var massTarget = ns.State.liveArmy(run.army)[0];
    var qualityTarget = run.army.find(function (stack) { return stack.unitId === "Rusher" && stack.amount >= 8; })
      || run.army.find(function (stack) { return stack.unitId === "Wisp" && stack.amount >= 10; });

    if (lost) {
      offers.push(makeOffer(
        "shop-recover",
        "Recovery",
        "Field Resurrection",
        "Recover a portion of the most damaged stack.",
        55,
        { type: "recovery", stackId: lost.id, amount: Math.max(1, Math.ceil(lost.lost * 0.6)) }
      ));
    } else {
      offers.push(makeOffer(
        "shop-tonic",
        "Recovery",
        "Reserve Tonic",
        "No losses to revive. Buy a small Rusher reinforcement instead.",
        45,
        { type: "mass", stackId: massTarget.id, amount: 6 }
      ));
    }

    if (skillReward) {
      offers.push(makeOffer(
        "shop-skill",
        "Skill",
        skillReward.label,
        "Adds one legal skill to one stack.",
        85,
        { type: "skill", stackId: skillReward.stackId, skillId: skillReward.skillId }
      ));
    }

    offers.push(makeOffer(
      "shop-stack",
      "Stack",
      "Hire Trappers",
      "Add a limited tactical role for the final route.",
      75,
      { type: "width", unitId: "Trapper", amount: 10, newStackId: ns.State.nextId("stack") }
    ));

    if (qualityTarget) {
      var to = qualityTarget.unitId === "Wisp" ? "StoneGolem" : "Axeman";
      var ratio = qualityTarget.unitId === "Wisp" ? 0.16 : 0.28;
      offers.push(makeOffer(
        "shop-upgrade",
        "Upgrade",
        "Controlled Promotion",
        "Trade quantity for a stronger class.",
        95,
        { type: "quality", stackId: qualityTarget.id, from: qualityTarget.unitId, to: to, amount: Math.max(1, Math.floor(qualityTarget.amount * ratio)) }
      ));
    }

    offers.push(makeOffer(
      "shop-economy",
      "Economy",
      "Sell Salvage",
      "Take a simple economy option without opening a full optimizer.",
      0,
      { type: "economy", gold: 55 }
    ));

    return {
      id: ns.State.nextId("choice"),
      nodeId: node.id,
      title: node.title,
      offers: offers
    };
  }

  function previewOffer(run, offer) {
    return ns.Rewards.applyOperation(run, offer.operation, ns.State.clone(run.army), run.gold - offer.cost);
  }

  function buyOffer(run, offer) {
    if (offer.purchased || run.gold < offer.cost) {
      return false;
    }
    run.gold -= offer.cost;
    ns.Rewards.applyOperation(run, offer.operation, run.army, run.gold);
    if (offer.operation.type === "economy") {
      run.gold += offer.operation.gold;
    }
    offer.purchased = true;
    run.shopHistory.push({
      title: offer.title,
      category: offer.category,
      cost: offer.cost,
      source: offer.resultSource
    });
    run.history.push({
      type: "shop",
      label: "Shop Purchase",
      detail: offer.title + " (" + offer.category + ")",
      gold: run.gold,
      value: ns.State.armyValue(run.army)
    });
    return true;
  }

  ns.Shop = {
    buildShopVisit: buildShopVisit,
    previewOffer: previewOffer,
    buyOffer: buyOffer
  };
})(window.Retsot = window.Retsot || {});

(function (ns) {
  "use strict";

  var catalog = [
    { id: "mass-rusher", family: "Mass", verb: "Grow", rarity: "Common", unitId: "Rusher", amount: 12 },
    { id: "mass-thrower", family: "Mass", verb: "Grow", rarity: "Common", unitId: "Thrower", amount: 5 },
    { id: "mass-wisp", family: "Mass", verb: "Grow", rarity: "Common", unitId: "Wisp", amount: 22 },
    { id: "recovery-revive", family: "Recovery", verb: "Revive", rarity: "Common", percent: 0.45 },
    { id: "economy-gold", family: "Economy", verb: "Earn", rarity: "Common", gold: 90 },
    { id: "width-trapper", family: "Width", verb: "Add", rarity: "Uncommon", unitId: "Trapper", amount: 12 },
    { id: "width-stonegolem", family: "Width", verb: "Add", rarity: "Uncommon", unitId: "StoneGolem", amount: 5 },
    { id: "width-fire", family: "Width", verb: "Add", rarity: "Rare", unitId: "FireElemental", amount: 2 },
    { id: "quality-rusher-axeman", family: "Quality", verb: "Promote", rarity: "Uncommon", from: "Rusher", to: "Axeman", ratio: 0.28 },
    { id: "quality-wisp-stone", family: "Quality", verb: "Promote", rarity: "Uncommon", from: "Wisp", to: "StoneGolem", ratio: 0.16 },
    { id: "quality-thrower-specialist", family: "Quality", verb: "Promote", rarity: "Rare", from: "Thrower", to: "Specialist", ratio: 0.38 },
    { id: "skill-legal", family: "Skill", verb: "Teach", rarity: "Uncommon" }
  ];

  function highestValueStack(army) {
    return ns.State.liveArmy(army).sort(function (a, b) {
      return ns.State.stackValue(b) - ns.State.stackValue(a);
    })[0] || null;
  }

  function firstStackByUnit(army, unitId) {
    return ns.State.liveArmy(army).find(function (stack) {
      return stack.unitId === unitId;
    }) || null;
  }

  function biggestLossStack(army) {
    return ns.State.liveArmy(army).filter(function (stack) {
      return (stack.lost || 0) > 0;
    }).sort(function (a, b) {
      return (b.lost || 0) - (a.lost || 0);
    })[0] || null;
  }

  function makeCard(base, intention, run, stageBoost) {
    var army = run.army;
    var stack;
    var amount;
    var op;
    var title;
    var detail;
    var before;
    var after;
    var stage = Math.max(1, run.stage || 1);
    var boost = stageBoost || stage;

    if (base.family === "Recovery") {
      stack = biggestLossStack(army);
      if (!stack) {
        return makeCard({ family: "Economy", verb: "Earn", rarity: "Common", gold: 70 }, intention, run, boost);
      }
      amount = Math.max(1, Math.ceil((stack.lost || 0) * base.percent));
      op = { type: "recovery", stackId: stack.id, amount: amount };
      before = stack.amount + " alive, " + stack.lost + " lost";
      after = (stack.amount + amount) + " alive, " + Math.max(0, stack.lost - amount) + " lost";
      title = "Revive " + ns.UNITS[stack.unitId].name;
      detail = "Recover losses from the last battles.";
    } else if (base.family === "Economy") {
      amount = base.gold + boost * 15;
      op = { type: "economy", gold: amount };
      before = run.gold + " RUN GOLD";
      after = (run.gold + amount) + " RUN GOLD";
      title = "Earn Run Gold";
      detail = "Delay power for a stronger shop decision.";
    } else if (base.family === "Skill") {
      var skillReward = ns.SkillRuntime.findLegalSkillReward(army);
      if (!skillReward) {
        return makeCard({ family: "Width", verb: "Add", rarity: "Uncommon", unitId: "Trapper", amount: 10 }, intention, run, boost);
      }
      stack = ns.State.findStack(army, skillReward.stackId);
      op = { type: "skill", stackId: stack.id, skillId: skillReward.skillId };
      before = ns.UNITS[stack.unitId].name + " skills: " + stack.skills.filter(function (s) { return s.unlocked; }).length;
      after = "Gains " + skillReward.skillId;
      title = skillReward.label;
      detail = "Skill belongs to this unit stack, not to the whole army.";
    } else if (base.family === "Width") {
      amount = base.amount + Math.max(0, boost - 1) * 2;
      op = { type: "width", unitId: base.unitId, amount: amount, newStackId: ns.State.nextId("stack") };
      before = "No " + ns.UNITS[base.unitId].name + " stack";
      after = "Add " + ns.UNITS[base.unitId].name + " x" + amount;
      title = "Add " + ns.UNITS[base.unitId].name;
      detail = "Open a new tactical role without editing the whole army.";
    } else if (base.family === "Quality") {
      stack = firstStackByUnit(army, base.from);
      if (!stack || stack.amount < 4) {
        return makeCard({ family: "Skill", verb: "Teach", rarity: "Uncommon" }, intention, run, boost);
      }
      amount = Math.max(1, Math.floor(stack.amount * base.ratio));
      op = { type: "quality", stackId: stack.id, from: base.from, to: base.to, amount: amount };
      before = ns.UNITS[base.from].name + " x" + stack.amount;
      after = ns.UNITS[base.to].name + " x" + amount;
      title = "Promote " + ns.UNITS[base.from].name;
      detail = "Less mass, stronger stack identity.";
    } else {
      stack = firstStackByUnit(army, base.unitId) || highestValueStack(army);
      amount = base.amount + Math.max(0, boost - 1) * 3;
      op = { type: "mass", stackId: stack.id, amount: amount };
      before = ns.UNITS[stack.unitId].name + " x" + stack.amount;
      after = ns.UNITS[stack.unitId].name + " x" + (stack.amount + amount);
      title = "Grow " + ns.UNITS[stack.unitId].name;
      detail = "Immediate mass for an existing stack.";
    }

    return {
      id: ns.State.nextId("choice"),
      templateId: base.id || (base.family + "-generated"),
      intention: intention,
      family: base.family,
      verb: base.verb,
      rarity: base.rarity,
      title: title,
      detail: detail,
      before: before,
      after: after,
      operation: op,
      resultSource: "offline-local-reward-resolver"
    };
  }

  function buildRewardChoice(run, source) {
    var stage = Math.max(1, run.stage || 1);
    var strengthenBase = stage % 2 === 0 ? catalog[8] : catalog[0];
    var pivotBase = run.routeBias.indexOf("Skill") !== -1 ? catalog[11] : catalog[5 + (stage % 3)];
    var stabilizeBase = biggestLossStack(run.army) ? catalog[3] : catalog[4];

    return {
      id: ns.State.nextId("choice"),
      source: source || "battle",
      battleSummary: run.lastBattle ? ns.State.clone(run.lastBattle) : null,
      gained: run.lastBattle ? { gold: run.lastBattle.goldGained || 0 } : { gold: 0 },
      cards: [
        makeCard(stabilizeBase, "Stabilize", run, stage),
        makeCard(strengthenBase, "Strengthen", run, stage),
        makeCard(pivotBase, "Pivot", run, stage)
      ]
    };
  }

  function applyOperation(run, operation, previewArmy, previewGold) {
    var army = previewArmy || run.army;
    var gold = typeof previewGold === "number" ? previewGold : run.gold;
    var stack;

    if (operation.type === "mass") {
      stack = ns.State.findStack(army, operation.stackId);
      if (stack) {
        stack.amount += operation.amount;
      }
    }

    if (operation.type === "recovery") {
      stack = ns.State.findStack(army, operation.stackId);
      if (stack) {
        ns.Combat.reviveStack(stack, operation.amount);
      }
    }

    if (operation.type === "economy") {
      gold += operation.gold;
    }

    if (operation.type === "skill") {
      stack = ns.State.findStack(army, operation.stackId);
      if (stack) {
        ns.State.addSkill(stack, operation.skillId);
      }
    }

    if (operation.type === "width") {
      army.push(ns.State.makeStack(operation.unitId, operation.amount, {
        id: operation.newStackId,
        unlockedSkills: [ns.UNITS[operation.unitId].skills[0]],
        origin: "reward"
      }));
    }

    if (operation.type === "quality") {
      stack = ns.State.findStack(army, operation.stackId);
      if (stack) {
        stack.unitId = operation.to;
        stack.amount = operation.amount;
        stack.lost = 0;
        stack.tempHp = ns.UNITS[operation.to].hp;
        stack.tier = ns.UNITS[operation.to].tier;
        stack.skills = ns.State.makeSkillState(operation.to, [ns.UNITS[operation.to].skills[0]]);
      }
    }

    return { army: army, gold: gold };
  }

  function previewCard(run, card) {
    return applyOperation(run, card.operation, ns.State.clone(run.army), run.gold);
  }

  function confirmCard(run, card) {
    var result = applyOperation(run, card.operation, run.army, run.gold);
    run.gold = result.gold;
    run.rewardHistory.push({
      title: card.title,
      family: card.family,
      intention: card.intention,
      before: card.before,
      after: card.after,
      source: card.resultSource
    });
    run.history.push({
      type: "reward",
      label: card.intention + " Reward",
      detail: card.title + ": " + card.before + " -> " + card.after,
      gold: run.gold,
      value: ns.State.armyValue(run.army)
    });
  }

  ns.Rewards = {
    catalog: catalog,
    buildRewardChoice: buildRewardChoice,
    previewCard: previewCard,
    confirmCard: confirmCard,
    applyOperation: applyOperation
  };
})(window.Retsot = window.Retsot || {});

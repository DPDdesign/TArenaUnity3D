(function (ns) {
  "use strict";

  var riskLoss = {
    None: 0,
    Low: 0.06,
    Medium: 0.1,
    High: 0.15,
    Severe: 0.2
  };

  function createBattleContext(run, node) {
    return {
      id: ns.State.nextId("choice"),
      runId: run.id,
      routeNodeId: node.id,
      nodeTitle: node.title,
      nodeType: node.type,
      enemyGoal: node.enemyGoal,
      risk: node.risk,
      recommendedValue: node.recommendedValue,
      currentValue: ns.State.armyValue(run.army),
      resultSource: "offline-local-battle-adapter",
      status: "ready"
    };
  }

  function resolveBattle(run, battle) {
    var before = ns.State.clone(run.army);
    var value = ns.State.armyValue(run.army);
    var recommended = battle.recommendedValue || 1;
    var ratio = value / recommended;
    var isFinal = battle.nodeType === "final";
    var baseLoss = riskLoss[battle.risk] || 0.08;
    var pressure = battle.enemyGoal === "deal maximum losses" ? 0.045 : 0.015;
    var underdog = ratio < 1 ? (1 - ratio) * 0.09 : 0;
    var control = ratio > 1 ? Math.min(0.045, (ratio - 1) * 0.025) : 0;
    var lossRate = Math.max(0.025, baseLoss + pressure + underdog - control);
    var win = ratio >= (isFinal ? 0.74 : 0.58);
    var losses = [];

    if (isFinal && !run.preFinalSnapshot) {
      run.preFinalSnapshot = ns.State.clone(run.army);
    }

    ns.State.liveArmy(run.army).forEach(function (stack, index) {
      var unit = ns.UNITS[stack.unitId];
      var fragility = unit.hp <= 8 ? 1.25 : (unit.hp >= 100 ? 0.72 : 1);
      var nodeWeight = 1 + (index % 3) * 0.08;
      var amount = Math.ceil(stack.amount * lossRate * fragility * nodeWeight);
      if (win) {
        amount = Math.min(Math.max(0, stack.amount - 1), amount);
      } else {
        amount = Math.min(stack.amount, Math.max(amount, Math.ceil(stack.amount * 0.42)));
      }
      var killed = ns.Combat.applyStackLoss(stack, amount);
      if (killed > 0) {
        losses.push({ stackId: stack.id, unitId: stack.unitId, amount: killed });
      }
    });

    var goldGained = isFinal ? 0 : Math.round(55 + run.stage * 18 + (battle.risk === "High" ? 20 : 0));
    if (run.flags.investment) {
      goldGained += run.flags.investment;
      run.flags.investment = 0;
    }

    if (win) {
      run.gold += goldGained;
    }

    var result = {
      battleId: battle.id,
      nodeTitle: battle.nodeTitle,
      nodeType: battle.nodeType,
      enemyGoal: battle.enemyGoal,
      risk: battle.risk,
      result: win ? "win" : "loss",
      beforeValue: ns.State.armyValue(before),
      afterValue: ns.State.armyValue(run.army),
      losses: losses,
      goldGained: win ? goldGained : 0,
      source: battle.resultSource
    };

    run.lastBattle = result;
    run.battleResults.push(result);
    run.history.push({
      type: isFinal ? "final" : "battle",
      label: isFinal ? "Final Encounter" : "Run Battle",
      detail: battle.nodeTitle + ": " + result.result + ", losses " + losses.reduce(function (sum, loss) { return sum + loss.amount; }, 0),
      gold: run.gold,
      value: ns.State.armyValue(run.army)
    });

    return result;
  }

  function simulateAsyncResult(account, savedArmy, slotIndex) {
    var playerValue = ns.State.armyValue(savedArmy.army);
    var opponentValue = Math.round(playerValue * 0.94 + 180);
    var opponentRank = account.rank + (opponentValue > playerValue ? 35 : -20);
    var expected = 1 / (1 + Math.pow(10, (opponentRank - account.rank) / 400));
    var score = playerValue >= opponentValue * 0.9 ? 1 : 0;
    var delta = Math.round(32 * (score - expected));
    var xp = score ? 90 : 35;
    var beforeRank = account.rank;
    var beforeXp = account.xp;

    account.rank += delta;
    account.xp += xp;
    while (account.xp >= account.xpToNext) {
      account.xp -= account.xpToNext;
      account.xpToNext += 120;
      account.unlockedSlots = Math.min(5, account.unlockedSlots + 1);
    }

    var result = {
      id: ns.State.nextId("result"),
      slotIndex: slotIndex,
      savedArmyId: savedArmy.id,
      opponent: "Offline PvE Mirror",
      opponentValue: opponentValue,
      opponentRank: opponentRank,
      result: score ? "offence win" : "offence loss",
      rankBefore: beforeRank,
      rankAfter: account.rank,
      rankDelta: delta,
      xpBefore: beforeXp,
      xpGained: xp,
      xpAfter: account.xp,
      noStealNoDestroy: true,
      source: "offline-local-async-result-adapter"
    };

    savedArmy.attackHistory = savedArmy.attackHistory || [];
    savedArmy.attackHistory.unshift(result);
    account.resultHistory.unshift(result);
    account.resultHistory = account.resultHistory.slice(0, 20);
    return result;
  }

  ns.AI = {
    createBattleContext: createBattleContext,
    resolveBattle: resolveBattle,
    simulateAsyncResult: simulateAsyncResult
  };
})(window.Retsot = window.Retsot || {});

(function (ns) {
  "use strict";

  function calculateDamage(attackerStack, defenderStack, modifier) {
    var attacker = ns.UNITS[attackerStack.unitId];
    var defender = ns.UNITS[defenderStack.unitId];
    var attack = attacker.attack;
    var defense = defender.defense;
    var diff = attack - defense;
    var multiplier;

    if (diff === 0) {
      multiplier = 1;
    } else if (diff > 0) {
      multiplier = 1 + diff * 0.04;
    } else {
      multiplier = 1 + diff * 0.014;
    }

    var baseDamage = attacker.damageMin * attackerStack.amount;
    return Math.max(1, Math.ceil(baseDamage * multiplier * (modifier || 1)));
  }

  function applyPureDamage(stack, damage) {
    var unit = ns.UNITS[stack.unitId];
    var totalHp = unit.hp * (stack.amount - 1) + (stack.tempHp || unit.hp);
    var nextHp = Math.max(0, totalHp - damage);
    var oldAmount = stack.amount;

    if (nextHp <= 0) {
      stack.amount = 0;
      stack.tempHp = 0;
    } else {
      stack.amount = Math.floor(nextHp / unit.hp);
      stack.tempHp = nextHp - stack.amount * unit.hp;
      if (stack.tempHp >= 1) {
        stack.amount += 1;
      } else {
        stack.tempHp = unit.hp;
      }
    }

    var killed = Math.max(0, oldAmount - stack.amount);
    stack.lost = (stack.lost || 0) + killed;
    return killed;
  }

  function applyStackLoss(stack, amount) {
    var loss = Math.min(stack.amount, Math.max(0, Math.floor(amount)));
    stack.amount -= loss;
    stack.lost = (stack.lost || 0) + loss;
    if (stack.amount <= 0) {
      stack.amount = 0;
      stack.tempHp = 0;
    }
    return loss;
  }

  function reviveStack(stack, amount) {
    var recovered = Math.min(stack.lost || 0, Math.max(0, Math.floor(amount)));
    stack.amount += recovered;
    stack.lost = Math.max(0, (stack.lost || 0) - recovered);
    if (recovered > 0 && stack.tempHp <= 0) {
      stack.tempHp = ns.UNITS[stack.unitId].hp;
    }
    return recovered;
  }

  ns.Combat = {
    calculateDamage: calculateDamage,
    applyPureDamage: applyPureDamage,
    applyStackLoss: applyStackLoss,
    reviveStack: reviveStack
  };
})(window.Retsot = window.Retsot || {});

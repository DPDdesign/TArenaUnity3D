(function (ns) {
  "use strict";

  var storageKey = "tarena.prd019.retsot-online.prototype.v1";
  var idCounters = { run: 1, stack: 1, choice: 1, saved: 1, result: 1 };

  function clone(value) {
    return JSON.parse(JSON.stringify(value));
  }

  function nextId(prefix) {
    idCounters[prefix] = (idCounters[prefix] || 1) + 1;
    return prefix + "-" + idCounters[prefix];
  }

  function getUnit(unitId) {
    return ns.UNITS[unitId];
  }

  function makeSkillState(unitId, unlockedIds) {
    var unit = getUnit(unitId);
    var allowed = unlockedIds || unit.skills;
    return unit.skills.map(function (skillId) {
      return {
        id: skillId,
        unlocked: allowed.indexOf(skillId) !== -1,
        native: true
      };
    });
  }

  function makeStack(unitId, amount, options) {
    var unit = getUnit(unitId);
    var cfg = options || {};
    return {
      id: cfg.id || nextId("stack"),
      unitId: unitId,
      amount: amount,
      lost: cfg.lost || 0,
      tier: cfg.tier || unit.tier,
      level: cfg.level || 1,
      tempHp: unit.hp,
      skills: cfg.skills ? clone(cfg.skills) : makeSkillState(unitId, cfg.unlockedSkills),
      origin: cfg.origin || "run"
    };
  }

  function hasSkill(stack, skillId) {
    return stack.skills.some(function (skill) {
      return skill.id === skillId && skill.unlocked;
    });
  }

  function addSkill(stack, skillId) {
    var existing = stack.skills.find(function (skill) { return skill.id === skillId; });
    if (existing) {
      existing.unlocked = true;
      return;
    }

    stack.skills.push({ id: skillId, unlocked: true, native: false });
  }

  function stackValue(stack) {
    var unit = getUnit(stack.unitId);
    var skillValue = stack.skills.filter(function (skill) { return skill.unlocked; }).length * 18;
    var tierValue = stack.level > 1 ? (stack.level - 1) * 12 : 0;
    return Math.round(stack.amount * unit.cost + skillValue + tierValue);
  }

  function armyValue(army) {
    return army.reduce(function (sum, stack) {
      return sum + stackValue(stack);
    }, 0);
  }

  function liveArmy(army) {
    return army.filter(function (stack) {
      return stack.amount > 0;
    });
  }

  function findStack(army, stackId) {
    return army.find(function (stack) {
      return stack.id === stackId;
    });
  }

  function stackLabel(stack) {
    var unit = getUnit(stack.unitId);
    return unit.name + " x" + stack.amount;
  }

  var STARTING_ARMIES = [
    {
      id: "barbarian-starter",
      name: "Barbarian Starter",
      subtitle: "Direct melee pressure with a small support line.",
      gold: 120,
      stacks: [
        { unitId: "Rusher", amount: 28, unlockedSkills: ["Chope"] },
        { unitId: "Thrower", amount: 10, unlockedSkills: ["Range_Stance_Barb", "Double_Throw"] },
        { unitId: "Healer", amount: 5, unlockedSkills: ["Tough_Skin"] },
        { unitId: "Wisp", amount: 22, unlockedSkills: ["Blind_by_light"] }
      ]
    },
    {
      id: "lizard-breakout",
      name: "Lizard Breakout",
      subtitle: "Traps, repositioning, and a stronger mid-run pivot.",
      gold: 110,
      stacks: [
        { unitId: "Trapper", amount: 24, unlockedSkills: ["Range_Stance_Lizard", "Spike_Trap"] },
        { unitId: "Healer", amount: 6, unlockedSkills: ["Tough_Skin"] },
        { unitId: "Specialist", amount: 3, unlockedSkills: ["Force_Pull"] },
        { unitId: "Wisp", amount: 18, unlockedSkills: ["Blind_by_light"] }
      ]
    },
    {
      id: "stone-spark",
      name: "Stone Spark",
      subtitle: "Low mass start with one tough stack and many fragile bodies.",
      gold: 145,
      stacks: [
        { unitId: "Wisp", amount: 34, unlockedSkills: ["Blind_by_light"] },
        { unitId: "StoneGolem", amount: 7, unlockedSkills: ["Stone_Throw"] },
        { unitId: "Rusher", amount: 18, unlockedSkills: ["Chope"] },
        { unitId: "Healer", amount: 4, unlockedSkills: ["Tough_Skin"] }
      ]
    }
  ];

  var ROUTES = [
    {
      id: "iron-line",
      name: "Iron Line",
      description: "Steady battles, one shop, balanced reward hints.",
      recommendedValue: 1650,
      bias: ["Mass", "Recovery"],
      nodes: [
        { id: "n1", type: "battle", title: "Border Clash", risk: "Low", possibleRewards: ["Mass", "Recovery"], enemyGoal: "try to win", recommendedValue: 1450 },
        { id: "n2", type: "reward", title: "Supply Cache", risk: "None", possibleRewards: ["Economy", "Width"], enemyGoal: "none", recommendedValue: 0 },
        { id: "n3", type: "shop", title: "Run Shop", risk: "None", possibleRewards: ["Recovery", "Skill", "Upgrade"], enemyGoal: "none", recommendedValue: 0 },
        { id: "n4", type: "battle", title: "Hill Ambush", risk: "Medium", possibleRewards: ["Quality", "Skill"], enemyGoal: "deal maximum losses", recommendedValue: 2050 },
        { id: "n5", type: "final", title: "Final Proof", risk: "High", possibleRewards: ["Saved Army"], enemyGoal: "try to win", recommendedValue: 2650 }
      ]
    },
    {
      id: "relic-trail",
      name: "Relic Trail",
      description: "Earlier pivot reward, then shop into a heavier final.",
      recommendedValue: 1550,
      bias: ["Skill", "Width"],
      nodes: [
        { id: "n1", type: "reward", title: "Old Relic", risk: "None", possibleRewards: ["Skill", "Economy"], enemyGoal: "none", recommendedValue: 0 },
        { id: "n2", type: "battle", title: "Ridge Guard", risk: "Medium", possibleRewards: ["Skill", "Mass"], enemyGoal: "try to win", recommendedValue: 1700 },
        { id: "n3", type: "shop", title: "Run Shop", risk: "None", possibleRewards: ["Skill", "Stack", "Recovery"], enemyGoal: "none", recommendedValue: 0 },
        { id: "n4", type: "battle", title: "Ash Patrol", risk: "Medium", possibleRewards: ["Quality", "Recovery"], enemyGoal: "deal maximum losses", recommendedValue: 2150 },
        { id: "n5", type: "final", title: "Final Proof", risk: "High", possibleRewards: ["Saved Army"], enemyGoal: "try to win", recommendedValue: 2750 }
      ]
    },
    {
      id: "risk-road",
      name: "Risk Road",
      description: "More pressure before the shop, better growth if losses are controlled.",
      recommendedValue: 1800,
      bias: ["Quality", "Economy"],
      nodes: [
        { id: "n1", type: "battle", title: "Fast Raid", risk: "Medium", possibleRewards: ["Economy", "Mass"], enemyGoal: "deal maximum losses", recommendedValue: 1650 },
        { id: "n2", type: "battle", title: "Second Push", risk: "Medium", possibleRewards: ["Quality", "Skill"], enemyGoal: "try to win", recommendedValue: 1950 },
        { id: "n3", type: "reward", title: "Spoils Split", risk: "None", possibleRewards: ["Width", "Economy"], enemyGoal: "none", recommendedValue: 0 },
        { id: "n4", type: "shop", title: "Last Camp", risk: "None", possibleRewards: ["Recovery", "Upgrade", "Skill"], enemyGoal: "none", recommendedValue: 0 },
        { id: "n5", type: "final", title: "Final Proof", risk: "Severe", possibleRewards: ["Saved Army"], enemyGoal: "try to win", recommendedValue: 2950 }
      ]
    }
  ];

  function createArmyFromTemplate(templateStacks) {
    return templateStacks.map(function (entry) {
      return makeStack(entry.unitId, entry.amount, {
        unlockedSkills: entry.unlockedSkills,
        level: entry.level || 1,
        tier: entry.tier
      });
    });
  }

  function createRun(startingArmyId, routeId) {
    var starting = STARTING_ARMIES.find(function (item) { return item.id === startingArmyId; }) || STARTING_ARMIES[0];
    var route = ROUTES.find(function (item) { return item.id === routeId; }) || ROUTES[0];
    var army = createArmyFromTemplate(starting.stacks);
    var run = {
      id: nextId("run"),
      mode: "Offline",
      authority: "local-offline-adapter",
      startingArmyId: starting.id,
      routeId: route.id,
      routeName: route.name,
      routeBias: clone(route.bias),
      nodes: clone(route.nodes).map(function (node, index) {
        node.index = index;
        node.state = index === 0 ? "available" : "locked";
        return node;
      }),
      nodeIndex: -1,
      gold: starting.gold,
      stage: 1,
      status: "active",
      army: army,
      startSnapshot: clone(army),
      preFinalSnapshot: null,
      history: [],
      rewardHistory: [],
      shopHistory: [],
      battleResults: [],
      lastBattle: null,
      activeNodeId: null,
      flags: {}
    };

    run.history.push({
      type: "start",
      label: "Run Started",
      detail: starting.name + " on " + route.name,
      gold: run.gold,
      value: armyValue(run.army)
    });

    return run;
  }

  function defaultAccount() {
    return {
      playerId: "offline-player",
      rank: 1000,
      xp: 0,
      xpToNext: 300,
      unlockedSlots: 2,
      currentDefenseSlot: null,
      savedArmies: [null, null, null, null, null, null, null, null],
      resultHistory: []
    };
  }

  function normalizeAccount(account) {
    var base = defaultAccount();
    var merged = Object.assign(base, account || {});
    if (!Array.isArray(merged.savedArmies)) {
      merged.savedArmies = base.savedArmies;
    }
    while (merged.savedArmies.length < 8) {
      merged.savedArmies.push(null);
    }
    merged.savedArmies = merged.savedArmies.slice(0, 8);
    return merged;
  }

  function loadAccount() {
    try {
      if (!window.localStorage) {
        return defaultAccount();
      }
      return normalizeAccount(JSON.parse(localStorage.getItem(storageKey)));
    } catch (error) {
      return defaultAccount();
    }
  }

  function saveAccount(account) {
    try {
      if (window.localStorage) {
        localStorage.setItem(storageKey, JSON.stringify(normalizeAccount(account)));
      }
    } catch (error) {
      // Static file mode may block localStorage; the run can still continue in memory.
    }
  }

  function createAppState() {
    return {
      screen: "start",
      account: loadAccount(),
      selectedStartingArmyId: STARTING_ARMIES[0].id,
      selectedRouteId: ROUTES[0].id,
      selectedNodeId: null,
      selectedSlot: 0,
      pendingOverwriteSlot: null,
      run: null,
      activeBattle: null,
      activeRewards: null,
      focusedRewardId: null,
      activeShop: null,
      focusedOfferId: null,
      asyncResult: null,
      mockupRunCreated: false,
      message: "Offline Mode prototype. Future Online Mode must validate this flow server-side."
    };
  }

  ns.State = {
    STARTING_ARMIES: STARTING_ARMIES,
    ROUTES: ROUTES,
    clone: clone,
    nextId: nextId,
    getUnit: getUnit,
    makeStack: makeStack,
    makeSkillState: makeSkillState,
    hasSkill: hasSkill,
    addSkill: addSkill,
    stackValue: stackValue,
    armyValue: armyValue,
    liveArmy: liveArmy,
    findStack: findStack,
    stackLabel: stackLabel,
    createRun: createRun,
    createAppState: createAppState,
    loadAccount: loadAccount,
    saveAccount: saveAccount
  };
})(window.Retsot = window.Retsot || {});

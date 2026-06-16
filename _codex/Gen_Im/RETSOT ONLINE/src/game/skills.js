(function (ns) {
  "use strict";

  var SKILL_REWARD_POOL = [
    { unitId: "Rusher", skillId: "Hate", label: "Teach Hate to Rusher" },
    { unitId: "Rusher", skillId: "Tough_Skin", label: "Teach Tough Skin to Rusher" },
    { unitId: "Thrower", skillId: "Fire_Ball", label: "Teach Fire Ball to Thrower" },
    { unitId: "Thrower", skillId: "Rush", label: "Teach Rush to Thrower" },
    { unitId: "Healer", skillId: "Blind_by_light", label: "Teach Blind to Healer" },
    { unitId: "Wisp", skillId: "Stone_Skin", label: "Teach Stone Skin to Wisp" },
    { unitId: "Trapper", skillId: "Blind_by_light", label: "Teach Blind to Trapper" },
    { unitId: "Specialist", skillId: "Defence_Ritual", label: "Teach Defence Ritual to Specialist" },
    { unitId: "StoneGolem", skillId: "Tough_Skin", label: "Teach Tough Skin to Stone Golem" },
    { unitId: "FireElemental", skillId: "Toxic_Fume", label: "Teach Toxic Fume to Fire Elemental" }
  ];

  function skillIcon(skillId) {
    return "assets/skills/" + (ns.SKILL_ICON_FILES[skillId] || (skillId + ".png"));
  }

  function unitIcon(unitId) {
    return "assets/units/" + ns.UNITS[unitId].sprite;
  }

  function findLegalSkillReward(army, skipSkillId) {
    for (var i = 0; i < SKILL_REWARD_POOL.length; i += 1) {
      var candidate = SKILL_REWARD_POOL[i];
      if (candidate.skillId === skipSkillId) {
        continue;
      }
      var stack = army.find(function (item) {
        return item.unitId === candidate.unitId && item.amount > 0 && !ns.State.hasSkill(item, candidate.skillId);
      });
      if (stack) {
        return Object.assign({ stackId: stack.id }, candidate);
      }
    }
    return null;
  }

  ns.SkillRuntime = {
    SKILL_REWARD_POOL: SKILL_REWARD_POOL,
    skillIcon: skillIcon,
    unitIcon: unitIcon,
    findLegalSkillReward: findLegalSkillReward
  };
})(window.Retsot = window.Retsot || {});

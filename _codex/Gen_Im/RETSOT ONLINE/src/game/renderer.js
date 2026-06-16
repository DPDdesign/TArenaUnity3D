(function (ns) {
  "use strict";

  function esc(value) {
    return String(value == null ? "" : value)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function fmt(value) {
    return Math.round(value).toLocaleString("en-US");
  }

  function skillIcons(stack) {
    return '<div class="skill-icons">' + stack.skills.map(function (skill) {
      var title = skill.id + (skill.unlocked ? "" : " locked");
      return '<img class="skill-icon' + (skill.unlocked ? "" : " locked") + '" src="' + ns.SkillRuntime.skillIcon(skill.id) + '" title="' + esc(title) + '" alt="' + esc(skill.id) + '">';
    }).join("") + "</div>";
  }

  function armyRows(army, options) {
    var cfg = options || {};
    if (!army || army.length === 0) {
      return '<div class="empty">No army stacks.</div>';
    }

    return '<div class="stack-list">' + army.map(function (stack) {
      var unit = ns.UNITS[stack.unitId];
      var css = "stack-row";
      if (cfg.preview) {
        css += " preview";
      }
      if ((stack.lost || 0) > 0) {
        css += " loss";
      }
      return [
        '<div class="' + css + '">',
        '<img class="unit-img" src="' + ns.SkillRuntime.unitIcon(stack.unitId) + '" alt="' + esc(unit.name) + '">',
        '<div>',
        '<div class="stack-name">' + esc(unit.name) + ' x' + fmt(stack.amount) + '</div>',
        '<div class="stack-meta">Tier ' + esc(stack.tier || unit.tier) + ' / Level ' + fmt(stack.level || 1) + ' / Lost ' + fmt(stack.lost || 0) + '</div>',
        '<div class="stack-meta">ATK ' + unit.attack + ' DEF ' + unit.defense + ' INI ' + unit.initiative + ' SPD ' + unit.speed + '</div>',
        skillIcons(stack),
        '</div>',
        '<div class="value-box"><strong>' + fmt(ns.State.stackValue(stack)) + '</strong><span class="dim">value</span></div>',
        '</div>'
      ].join("");
    }).join("") + "</div>";
  }

  function armyPanel(title, army, extra) {
    return [
      '<section class="panel">',
      '<div class="panel-header"><h2>' + esc(title) + '</h2><span class="tag gold">' + fmt(ns.State.armyValue(army || [])) + ' value</span></div>',
      '<div class="panel-body">',
      extra ? '<p class="muted">' + esc(extra) + '</p>' : '',
      armyRows(army || []),
      '</div>',
      '</section>'
    ].join("");
  }

  function log(history) {
    if (!history || history.length === 0) {
      return '<div class="empty">No run history yet.</div>';
    }
    return '<div class="log">' + history.slice().reverse().map(function (entry) {
      return '<div class="log-entry"><strong>' + esc(entry.label) + '</strong><br>' + esc(entry.detail) + '<br><span class="dim">Gold ' + fmt(entry.gold || 0) + ' / Value ' + fmt(entry.value || 0) + '</span></div>';
    }).join("") + "</div>";
  }

  function topbar(app) {
    var run = app.run;
    var value = run ? ns.State.armyValue(run.army) : 0;
    return [
      '<header class="topbar">',
      '<nav class="mockup-menu" aria-label="Prototype menu">',
      '<button class="menu-button" data-action="noop" aria-label="Open menu">Menu</button>',
      '<div class="menu-popover">',
      '<button data-action="open-task20-mockup">Task 20 mockup</button>',
      '<button data-action="open-task21-mockup">Task 21 mockup</button>',
      '<button data-action="open-run-prototype">PRD019 run prototype</button>',
      '</div>',
      '</nav>',
      '<div class="brand"><h1>TArena PRD019 Offline Run</h1><p>Mewgenics-like run, Heroes 3-like battles. Reward flow first.</p></div>',
      '<span class="pill"><span>Mode</span><strong>Offline</strong></span>',
      '<span class="pill"><span>RUN GOLD</span><strong>' + fmt(run ? run.gold : 0) + '</strong></span>',
      '<span class="pill"><span>Army</span><strong>' + fmt(value) + '</strong></span>',
      '<span class="pill"><span>Rank</span><strong>' + fmt(app.account.rank) + '</strong></span>',
      '</header>'
    ].join("");
  }

  function nodeIcon(type) {
    if (type === "battle") { return "Battle"; }
    if (type === "shop") { return "Shop"; }
    if (type === "reward") { return "Reward"; }
    if (type === "final") { return "Final"; }
    return "Node";
  }

  function slots(account, selectedSlot) {
    return '<div class="slots">' + account.savedArmies.map(function (army, index) {
      var unlocked = index < account.unlockedSlots;
      var state = !unlocked ? "Locked" : (army ? "Taken" : "Empty");
      var css = "slot" + (selectedSlot === index ? " selected" : "") + (!unlocked ? " locked" : "");
      var defence = account.currentDefenseSlot === index ? '<span class="tag green">Defence</span>' : "";
      return [
        '<button class="' + css + '" data-action="select-slot" data-slot="' + index + '">',
        '<h3>Slot ' + (index + 1) + '</h3>',
        '<div class="muted">' + state + '</div>',
        army ? '<div class="stack-meta">' + esc(army.name) + '<br>' + fmt(ns.State.armyValue(army.army)) + ' value</div>' : '',
        defence,
        '</button>'
      ].join("");
    }).join("") + "</div>";
  }

  function routeNodes(run, selectedNodeId) {
    var nextIndex = run.nodeIndex + 1;
    return '<div class="route">' + run.nodes.map(function (node, index) {
      var state = index < nextIndex ? "completed" : (index === nextIndex ? "available" : "locked");
      node.state = state;
      var css = "node " + state + (selectedNodeId === node.id ? " selected" : "");
      var disabled = state !== "available" ? " disabled" : "";
      return [
        '<button class="' + css + '" data-action="select-node" data-node="' + esc(node.id) + '"' + disabled + '>',
        '<span class="tag ' + (node.type === "final" ? "red" : "green") + '">' + nodeIcon(node.type) + '</span>',
        '<h3>' + esc(node.title) + '</h3>',
        '<p>Risk: ' + esc(node.risk) + '</p>',
        '<p>Possible Rewards: ' + esc(node.possibleRewards.join(", ")) + '</p>',
        '<p>Recommended Value: ' + (node.recommendedValue ? fmt(node.recommendedValue) : "n/a") + '</p>',
        '</button>'
      ].join("");
    }).join("") + "</div>";
  }

  ns.Render = {
    esc: esc,
    fmt: fmt,
    skillIcons: skillIcons,
    armyRows: armyRows,
    armyPanel: armyPanel,
    log: log,
    topbar: topbar,
    slots: slots,
    routeNodes: routeNodes
  };
})(window.Retsot = window.Retsot || {});

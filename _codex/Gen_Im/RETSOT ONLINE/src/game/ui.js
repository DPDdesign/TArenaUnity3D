(function (ns) {
  "use strict";

  var app;
  var root;

  function selectedStartingArmy() {
    return ns.State.STARTING_ARMIES.find(function (item) {
      return item.id === app.selectedStartingArmyId;
    }) || ns.State.STARTING_ARMIES[0];
  }

  function selectedRoute() {
    return ns.State.ROUTES.find(function (item) {
      return item.id === app.selectedRouteId;
    }) || ns.State.ROUTES[0];
  }

  function getSelectedNode() {
    if (!app.run) {
      return null;
    }
    return app.run.nodes.find(function (node) {
      return node.id === app.selectedNodeId;
    }) || app.run.nodes[app.run.nodeIndex + 1] || null;
  }

  function getFocusedReward() {
    if (!app.activeRewards) {
      return null;
    }
    return app.activeRewards.cards.find(function (card) {
      return card.id === app.focusedRewardId;
    }) || app.activeRewards.cards[0];
  }

  function getFocusedOffer() {
    if (!app.activeShop) {
      return null;
    }
    return app.activeShop.offers.find(function (offer) {
      return offer.id === app.focusedOfferId;
    }) || app.activeShop.offers[0];
  }

  function markCurrentNodeComplete() {
    var node = getSelectedNode();
    if (!node || !app.run) {
      return;
    }
    app.run.nodeIndex = Math.max(app.run.nodeIndex, node.index);
    app.run.stage = Math.min(app.run.nodes.length, app.run.nodeIndex + 2);
    app.selectedNodeId = null;
  }

  function screenChrome(inner) {
    return ns.Render.topbar(app) + inner;
  }

  function scrollToMockup(id) {
    window.setTimeout(function () {
      var element = document.getElementById(id);
      if (element) {
        element.scrollIntoView({ behavior: "smooth", block: "start" });
      }
    }, 0);
  }

  function renderTask20Mockup() {
    var selected = selectedStartingArmy();
    var route = selectedRoute();
    var selectedArmy = ns.State.createRun(selected.id, route.id).startSnapshot;
    var currentValue = ns.State.armyValue(selectedArmy);
    return screenChrome([
      '<div class="screen mockup-page">',
      '<section class="mockup-hero">',
      '<span class="tag green">Task 20 mockup</span>',
      '<h2>Start Run: wybór słabszej armii startowej i trasy runu</h2>',
      '<p>Ten ekran pokazuje, co trzeba później złożyć w Unity: osobne Starting Armies, podgląd stacków, per-unit skills, trzy trasy oraz wynik komendy Begin Run. To nie jest Saved Armies i nie ma stanów offence/defence.</p>',
      '<div class="scene-jump">',
      '<button data-action="scroll-mockup" data-anchor="mockup-20-armies">Army + Inspector</button>',
      '<button data-action="scroll-mockup" data-anchor="mockup-20-routes">Route Preview</button>',
      '<button data-action="scroll-mockup" data-anchor="mockup-20-created">Begin Result</button>',
      '</div>',
      '</section>',
      '<section class="flow-strip">',
      '<div class="flow-step"><strong>1. Army + Inspector</strong><span class="muted">Gracz wybiera weaker Starting Army i od razu widzi jej stacki.</span></div>',
      '<div class="flow-step"><strong>2. Route</strong><span class="muted">Gracz wybiera jedną z 3 tras z recommended value.</span></div>',
      '<div class="flow-step"><strong>3. Begin</strong><span class="muted">Powstaje offline run state i initial army snapshot.</span></div>',
      '<div class="flow-step"><strong>Next task</strong><span class="muted">Kolejny task dostanie osobny page w menu.</span></div>',
      '</section>',
      '<section id="mockup-20-armies" class="panel mockup-section">',
      '<div class="panel-header"><h2>Scene 1 - Starting Army And Inspector</h2><span class="tag green">not saved armies</span></div>',
      '<div class="panel-body">',
      '<div class="grid-2">',
      '<div>',
      '<h3>Starting Armies</h3>',
      '<div class="stack-list">',
      ns.State.STARTING_ARMIES.map(function (army) {
        var previewRun = ns.State.createRun(army.id, route.id);
        var css = "card" + (army.id === app.selectedStartingArmyId ? " selected" : "");
        return [
          '<button class="' + css + '" data-action="select-mockup-starting" data-id="' + army.id + '">',
          '<div class="card-head"><h3>' + ns.Render.esc(army.name) + '</h3><span class="tag gold">' + ns.Render.fmt(ns.State.armyValue(previewRun.army)) + '</span></div>',
          '<div class="card-body">',
          '<p>' + ns.Render.esc(army.subtitle) + '</p>',
          '<p><span class="dim">Start gold</span><br>' + ns.Render.fmt(army.gold) + '</p>',
          '<span class="tag">Starting Army</span>',
          '</div>',
          '</button>'
        ].join("");
      }).join(""),
      '</div>',
      '</div>',
      '<div>',
      '<h3>Selected Army Inspector</h3>',
      '<div class="summary-strip">',
      '<div class="summary-item"><span>Selected template</span><strong>' + ns.Render.esc(selected.name) + '</strong></div>',
      '<div class="summary-item"><span>Stacks</span><strong>' + selectedArmy.length + '</strong></div>',
      '<div class="summary-item"><span>Run gold</span><strong>' + ns.Render.fmt(selected.gold) + '</strong></div>',
      '<div class="summary-item"><span>Use state badges</span><strong>None</strong></div>',
      '</div>',
      '<div class="grid-2 with-top-gap">',
      '<div>',
      '<h3>Stack rows</h3>',
      ns.Render.armyRows(selectedArmy),
      '</div>',
      '<div>',
      '<h3>UI rules</h3>',
      '<div class="payload-grid">',
      '<div class="payload-item"><strong>Show</strong>Tier/level, amount, stack value, total army value.</div>',
      '<div class="payload-item"><strong>Show</strong>Per-unit skill locked/unlocked indicators.</div>',
      '<div class="payload-item"><strong>Do not show</strong>Defence, offence, in use, saved-army slot state.</div>',
      '<div class="payload-item"><strong>Do not create</strong>Army-wide skills panel or shared army skill system.</div>',
      '</div>',
      '</div>',
      '</div>',
      '</div>',
      '</div>',
      '<div class="actions"><button class="primary" data-action="scroll-mockup" data-anchor="mockup-20-routes">Choose Route</button></div>',
      '</div>',
      '</section>',
      '<section id="mockup-20-routes" class="panel mockup-section">',
      '<div class="panel-header"><h2>Scene 2 - Route Preview</h2><span class="tag gold">3 choices</span></div>',
      '<div class="panel-body">',
      '<div class="grid-3">',
      ns.State.ROUTES.map(function (item) {
        var css = "card" + (item.id === app.selectedRouteId ? " selected" : "");
        return [
          '<button class="' + css + '" data-action="select-mockup-route" data-id="' + item.id + '">',
          '<div class="card-head"><h3>' + ns.Render.esc(item.name) + '</h3><span class="tag">' + ns.Render.fmt(item.recommendedValue) + '</span></div>',
          '<div class="card-body">',
          '<p>' + ns.Render.esc(item.description) + '</p>',
          '<p><span class="dim">Current army value</span><br>' + ns.Render.fmt(currentValue) + '</p>',
          '<p><span class="dim">Bias</span><br>' + ns.Render.esc(item.bias.join(", ")) + '</p>',
          '</div>',
          '</button>'
        ].join("");
      }).join(""),
      '</div>',
      '<div class="actions"><button class="primary" data-action="mockup-begin-run">Begin Run</button><button data-action="scroll-mockup" data-anchor="mockup-20-armies">Back To Armies</button></div>',
      '</div>',
      '</section>',
      '<section id="mockup-20-created" class="panel mockup-section">',
      '<div class="panel-header"><h2>Scene 3 - Begin Run Command Result</h2><span class="tag ' + (app.mockupRunCreated ? "green" : "gold") + '">' + (app.mockupRunCreated ? "created" : "preview") + '</span></div>',
      '<div class="panel-body">',
      '<div class="grid-2">',
      '<div>',
      '<h3>Player-facing result</h3>',
      app.mockupRunCreated ? '<div class="message">Run created from ' + ns.Render.esc(selected.name) + ' on ' + ns.Render.esc(route.name) + '. Unity can now transition to the Run Map page.</div>' : '<div class="empty">Click Begin Run in Scene 3 to show the created-run state.</div>',
      '<div class="actions"><button class="primary" data-action="begin-run">Open Current Run Prototype</button><button data-action="open-run-prototype">Back To Prototype Start</button></div>',
      '</div>',
      '<div>',
      '<h3>Mode-neutral payload</h3>',
      '<div class="payload-grid">',
      '<div class="payload-item"><strong>gameMode</strong>Offline now, Online later as separate adapter.</div>',
      '<div class="payload-item"><strong>startingArmyTemplateId</strong>' + ns.Render.esc(selected.id) + '</div>',
      '<div class="payload-item"><strong>routePreviewOptionId</strong>' + ns.Render.esc(route.id) + '</div>',
      '<div class="payload-item"><strong>initialArmySnapshot</strong>' + selectedArmy.length + ' stacks, ' + ns.Render.fmt(currentValue) + ' value.</div>',
      '<div class="payload-item"><strong>validation</strong>Missing, empty, invalid, or blocked run errors.</div>',
      '<div class="payload-item"><strong>createdRunId</strong>Returned by command result after Begin.</div>',
      '</div>',
      '</div>',
      '</div>',
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderTask21Mockup() {
    var selected = selectedStartingArmy();
    var route = selectedRoute();
    var mockRun = ns.State.createRun(selected.id, route.id);
    mockRun.gold = 120;
    mockRun.army[0].lost = 5;
    mockRun.army[2].lost = 2;
    var shop = ns.Shop.buildShopVisit(mockRun, { id: "mock-shop-node", title: "Run Shop" });
    var focused = shop.offers.find(function (offer) { return offer.id === app.mockupFocusedOfferId; }) || shop.offers[0];
    app.mockupFocusedOfferId = focused.id;
    var preview = ns.Shop.previewOffer(mockRun, focused);

    return screenChrome([
      '<div class="screen mockup-page">',
      '<section class="mockup-hero">',
      '<span class="tag green">Task 21 mockup</span>',
      '<h2>Run Shop: limited offers, RUN GOLD, preview before buy</h2>',
      '<p>Ten ekran pokazuje V1 shape dla shop node: osobny in-run wallet, limitowane oferty, podglad Army After Purchase, per-unit skills w shared army preview i brak pelnego army optimizera.</p>',
      '<div class="scene-jump">',
      '<button data-action="scroll-mockup" data-anchor="mockup-21-shop">Shop Screen</button>',
      '<button data-action="scroll-mockup" data-anchor="mockup-21-preview">Offer Preview</button>',
      '<button data-action="scroll-mockup" data-anchor="mockup-21-payload">Payload</button>',
      '</div>',
      '</section>',
      '<section class="flow-strip">',
      '<div class="flow-step"><strong>1. Inspect</strong><span class="muted">Gracz widzi armie, straty, skills i RUN GOLD.</span></div>',
      '<div class="flow-step"><strong>2. Focus offer</strong><span class="muted">Klik/focus oferty pokazuje Army After Purchase.</span></div>',
      '<div class="flow-step"><strong>3. Buy or leave</strong><span class="muted">Buy stosuje jedna operacje; Leave wraca do Run Map.</span></div>',
      '<div class="flow-step"><strong>Out of scope</strong><span class="muted">Brak metagame shopu, PlayFab, full army editor.</span></div>',
      '</section>',
      '<section id="mockup-21-shop" class="panel mockup-section">',
      '<div class="panel-header"><h2>Scene 1 - Run Shop</h2><span class="tag gold">RUN GOLD ' + ns.Render.fmt(mockRun.gold) + '</span></div>',
      '<div class="panel-body">',
      '<div class="grid-2">',
      ns.Render.armyPanel("Your Army", mockRun.army, "Shared army preview: wounds/losses and per-unit skills are visible."),
      '<div>',
      '<h3>Grouped Limited Offers</h3>',
      '<div class="grid-2">',
      shop.offers.map(function (offer) {
        var css = "card" + (offer.id === focused.id ? " focused" : "");
        return [
          '<button class="' + css + '" data-action="focus-mockup-offer" data-id="' + offer.id + '">',
          '<div class="card-head"><h3>' + ns.Render.esc(offer.title) + '</h3><span class="tag ' + (mockRun.gold >= offer.cost ? "green" : "red") + '">' + ns.Render.fmt(offer.cost) + '</span></div>',
          '<div class="card-body"><p>' + ns.Render.esc(offer.detail) + '</p><span class="tag gold">' + ns.Render.esc(offer.category) + '</span></div>',
          '</button>'
        ].join("");
      }).join(""),
      '</div>',
      '<div class="actions"><button class="primary" data-action="scroll-mockup" data-anchor="mockup-21-preview">Preview Focused Offer</button><button data-action="scroll-mockup" data-anchor="mockup-21-payload">Payload</button></div>',
      '</div>',
      '</div>',
      '</div>',
      '</section>',
      '<section id="mockup-21-preview" class="panel mockup-section">',
      '<div class="panel-header"><h2>Scene 2 - Offer Preview</h2><span class="tag gold">after purchase RUN GOLD ' + ns.Render.fmt(preview.gold) + '</span></div>',
      '<div class="panel-body">',
      '<div class="grid-2">',
      '<div>',
      '<h3>' + ns.Render.esc(focused.title) + '</h3>',
      '<div class="payload-grid">',
      '<div class="payload-item"><strong>category</strong>' + ns.Render.esc(focused.category) + '</div>',
      '<div class="payload-item"><strong>cost</strong>' + ns.Render.fmt(focused.cost) + ' RUN GOLD</div>',
      '<div class="payload-item"><strong>before</strong>' + ns.Render.esc(focused.detail) + '</div>',
      '<div class="payload-item"><strong>result source</strong>offline-local-shop-resolver</div>',
      '</div>',
      '<div class="actions"><button class="primary">Buy</button><button>Leave Shop</button></div>',
      '</div>',
      ns.Render.armyPanel("Army After Purchase", preview.army, "Preview must match the actual purchase result."),
      '</div>',
      '</div>',
      '</section>',
      '<section id="mockup-21-payload" class="panel mockup-section">',
      '<div class="panel-header"><h2>Scene 3 - Mode-neutral Payload</h2><span class="tag">offline now / online later</span></div>',
      '<div class="panel-body">',
      '<div class="payload-grid">',
      '<div class="payload-item"><strong>shopVisitId</strong>generated per shop node visit.</div>',
      '<div class="payload-item"><strong>offerId/category/cost</strong>limited authored/resolved offer data.</div>',
      '<div class="payload-item"><strong>focusedPreview</strong>army-after-purchase snapshot and currency after purchase.</div>',
      '<div class="payload-item"><strong>purchaseResult</strong>success, insufficient currency, invalid target, unavailable offer.</div>',
      '<div class="payload-item"><strong>backend gap</strong>Future Online validates offers and purchases server-side.</div>',
      '<div class="payload-item"><strong>Unity prefab</strong>Resources/UI/PRD_19_21.prefab plus Resources/UI/PRD19_21 templates.</div>',
      '</div>',
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderStart() {
    var selected = selectedStartingArmy();
    var selectedArmy = ns.State.createRun(selected.id, selectedRoute().id).startSnapshot;

    return screenChrome([
      '<div class="screen">',
      app.message ? '<div class="message">' + ns.Render.esc(app.message) + '</div>' : '',
      '<section class="grid-2">',
      '<div class="panel">',
      '<div class="panel-header"><h2>Starting Armies</h2><span class="tag green">separate from saved armies</span></div>',
      '<div class="panel-body grid-3">',
      ns.State.STARTING_ARMIES.map(function (army) {
        var previewRun = ns.State.createRun(army.id, selectedRoute().id);
        var css = "card" + (army.id === app.selectedStartingArmyId ? " selected" : "");
        return [
          '<button class="' + css + '" data-action="select-starting" data-id="' + army.id + '">',
          '<div class="card-head"><h3>' + ns.Render.esc(army.name) + '</h3><span class="tag gold">' + ns.Render.fmt(ns.State.armyValue(previewRun.army)) + '</span></div>',
          '<div class="card-body"><p>' + ns.Render.esc(army.subtitle) + '</p>',
          ns.Render.armyRows(previewRun.army),
          '</div></button>'
        ].join("");
      }).join(""),
      '</div>',
      '</div>',
      '<div class="panel">',
      '<div class="panel-header"><h2>Route Preview</h2><span class="tag gold">3 paths</span></div>',
      '<div class="panel-body">',
      '<div class="grid-3">',
      ns.State.ROUTES.map(function (route) {
        var css = "card" + (route.id === app.selectedRouteId ? " selected" : "");
        return [
          '<button class="' + css + '" data-action="select-route" data-id="' + route.id + '">',
          '<div class="card-head"><h3>' + ns.Render.esc(route.name) + '</h3><span class="tag">' + ns.Render.fmt(route.recommendedValue) + '</span></div>',
          '<div class="card-body"><p>' + ns.Render.esc(route.description) + '</p>',
          '<p><strong>Bias:</strong> ' + ns.Render.esc(route.bias.join(", ")) + '</p>',
          '<p><strong>Current army:</strong> ' + ns.Render.fmt(ns.State.armyValue(selectedArmy)) + '</p>',
          '</div></button>'
        ].join("");
      }).join(""),
      '</div>',
      '<div class="actions"><button class="primary" data-action="begin-run">Begin Run</button><button data-action="to-saved">Saved Armies</button></div>',
      '</div>',
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderMap() {
    var node = getSelectedNode();
    return screenChrome([
      '<div class="layout">',
      ns.Render.armyPanel("Your Army", app.run.army, "Shared army preview: unit tier, amount, value, and per-unit skills."),
      '<section class="panel">',
      '<div class="panel-header"><h2>Run Map - ' + ns.Render.esc(app.run.routeName) + '</h2><span class="tag green">Stage ' + app.run.stage + '</span></div>',
      '<div class="panel-body">',
      ns.Render.routeNodes(app.run, app.selectedNodeId),
      '<div class="actions"><button class="primary" data-action="travel-node"' + (node && node.state === "available" ? "" : " disabled") + '>Travel</button><button data-action="restart">New Run</button></div>',
      '</div>',
      '</section>',
      '<section class="panel">',
      '<div class="panel-header"><h2>Selected Node</h2><span class="tag gold">RUN GOLD ' + ns.Render.fmt(app.run.gold) + '</span></div>',
      '<div class="panel-body">',
      node ? [
        '<h3>' + ns.Render.esc(node.title) + '</h3>',
        '<p class="muted">Type: ' + ns.Render.esc(node.type) + '</p>',
        '<p class="muted">Expected Risk: ' + ns.Render.esc(node.risk) + '</p>',
        '<p class="muted">Possible Rewards: ' + ns.Render.esc(node.possibleRewards.join(", ")) + '</p>',
        '<p class="muted">Enemy Goal: ' + ns.Render.esc(node.enemyGoal) + '</p>'
      ].join("") : '<div class="empty">Select an available node.</div>',
      '<h3>Run Log</h3>',
      ns.Render.log(app.run.history),
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderBattle() {
    var battle = app.activeBattle;
    var cells = ns.Hex.demoCells();
    return screenChrome([
      '<div class="layout">',
      ns.Render.armyPanel("Current Run Army", app.run.army, "Battle is a PRD019 adapter here: it returns result, losses, and next screen."),
      '<section class="panel">',
      '<div class="panel-header"><h2>Run Battle Bridge</h2><span class="tag ' + (battle.nodeType === "final" ? "red" : "green") + '">' + ns.Render.esc(battle.nodeType) + '</span></div>',
      '<div class="panel-body">',
      '<div class="grid-2">',
      '<div><h3>' + ns.Render.esc(battle.nodeTitle) + '</h3><p class="muted">Enemy goal: ' + ns.Render.esc(battle.enemyGoal) + '</p><p class="muted">Expected risk: ' + ns.Render.esc(battle.risk) + '</p><p class="muted">Current value ' + ns.Render.fmt(battle.currentValue) + ' vs recommended ' + ns.Render.fmt(battle.recommendedValue) + '</p></div>',
      '<div class="battle-board">' + cells.map(function (cell) { return '<span class="hex ' + cell.type + '"></span>'; }).join("") + '</div>',
      '</div>',
      '<div class="actions"><button class="primary" data-action="resolve-battle">Resolve Local Battle Adapter</button><button data-action="to-map">Back To Map</button></div>',
      '</div>',
      '</section>',
      '<section class="panel">',
      '<div class="panel-header"><h2>Payload Boundary</h2><span class="tag">online-ready</span></div>',
      '<div class="panel-body">',
      '<p class="muted">Payload: run id, route node id, current army snapshot, encounter id, enemy goal, result, surviving stacks, losses, result source.</p>',
      '<p class="muted">Offline can resolve locally. Future Online must validate battle completion backend-side.</p>',
      ns.Render.log(app.run.history),
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderReward() {
    var focused = getFocusedReward();
    var preview = focused ? ns.Rewards.previewCard(app.run, focused) : { army: app.run.army, gold: app.run.gold };
    var battle = app.activeRewards.battleSummary;
    return screenChrome([
      '<div class="layout">',
      '<section class="panel">',
      '<div class="panel-header"><h2>Battle Result</h2><span class="tag green">' + (battle ? ns.Render.esc(battle.result) : "node reward") + '</span></div>',
      '<div class="panel-body">',
      battle ? '<p class="muted">' + ns.Render.esc(battle.nodeTitle) + ': ' + ns.Render.fmt(battle.losses.reduce(function (sum, loss) { return sum + loss.amount; }, 0)) + ' losses, gained ' + ns.Render.fmt(battle.goldGained) + ' RUN GOLD.</p>' : '<p class="muted">Route reward node. No battle result attached.</p>',
      '<p class="muted">Gained summary is separate from the 1-of-3 reward card choice.</p>',
      ns.Render.armyRows(app.run.army),
      '</div>',
      '</section>',
      '<section class="panel">',
      '<div class="panel-header"><h2>Choose 1 Of 3 Rewards</h2><span class="tag gold">preview first</span></div>',
      '<div class="panel-body">',
      '<div class="grid-3">',
      app.activeRewards.cards.map(function (card) {
        var css = "card" + (focused && focused.id === card.id ? " focused" : "");
        return [
          '<button class="' + css + '" data-action="focus-reward" data-id="' + card.id + '">',
          '<div class="card-head"><h3>' + ns.Render.esc(card.intention) + '</h3><span class="tag green">' + ns.Render.esc(card.family) + '</span></div>',
          '<div class="card-body">',
          '<p><strong>' + ns.Render.esc(card.verb) + '</strong> - ' + ns.Render.esc(card.title) + '</p>',
          '<p>' + ns.Render.esc(card.detail) + '</p>',
          '<p><span class="dim">Before</span><br>' + ns.Render.esc(card.before) + '</p>',
          '<p><span class="dim">After</span><br>' + ns.Render.esc(card.after) + '</p>',
          '<span class="tag gold">' + ns.Render.esc(card.rarity) + '</span>',
          '</div></button>'
        ].join("");
      }).join(""),
      '</div>',
      '<div class="actions"><button class="primary" data-action="confirm-reward"' + (focused ? "" : " disabled") + '>Select Reward And Continue</button></div>',
      '</div>',
      '</section>',
      '<section class="panel">',
      '<div class="panel-header"><h2>Army After Reward</h2><span class="tag gold">RUN GOLD ' + ns.Render.fmt(preview.gold) + '</span></div>',
      '<div class="panel-body">',
      ns.Render.armyRows(preview.army, { preview: true }),
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderShop() {
    var focused = getFocusedOffer();
    var preview = focused ? ns.Shop.previewOffer(app.run, focused) : { army: app.run.army, gold: app.run.gold };
    return screenChrome([
      '<div class="layout">',
      ns.Render.armyPanel("Your Army", app.run.army, "Same army preview component used across PRD019 screens."),
      '<section class="panel">',
      '<div class="panel-header"><h2>Run Shop</h2><span class="tag gold">RUN GOLD ' + ns.Render.fmt(app.run.gold) + '</span></div>',
      '<div class="panel-body">',
      '<div class="grid-2">',
      app.activeShop.offers.map(function (offer) {
        var css = "card" + (focused && focused.id === offer.id ? " focused" : "") + (offer.purchased ? " locked" : "");
        var affordable = app.run.gold >= offer.cost;
        return [
          '<button class="' + css + '" data-action="focus-offer" data-id="' + offer.id + '">',
          '<div class="card-head"><h3>' + ns.Render.esc(offer.title) + '</h3><span class="tag ' + (affordable ? "green" : "red") + '">' + ns.Render.fmt(offer.cost) + '</span></div>',
          '<div class="card-body"><p>' + ns.Render.esc(offer.detail) + '</p><span class="tag gold">' + ns.Render.esc(offer.category) + '</span>' + (offer.purchased ? '<span class="tag">Purchased</span>' : '') + '</div>',
          '</button>'
        ].join("");
      }).join(""),
      '</div>',
      '<div class="actions"><button class="primary" data-action="buy-offer"' + (!focused || focused.purchased || app.run.gold < focused.cost ? " disabled" : "") + '>Buy Focused Offer</button><button data-action="leave-shop">Leave Shop</button></div>',
      '</div>',
      '</section>',
      '<section class="panel">',
      '<div class="panel-header"><h2>Army After Purchase</h2><span class="tag gold">RUN GOLD ' + ns.Render.fmt(preview.gold) + '</span></div>',
      '<div class="panel-body">',
      focused ? '<p class="muted">' + ns.Render.esc(focused.title) + ' preview. Purchases are one concrete operation.</p>' : '',
      ns.Render.armyRows(preview.army, { preview: true }),
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderSummary() {
    var candidateArmy = app.run.preFinalSnapshot || app.run.army;
    var selectedArmy = app.account.savedArmies[app.selectedSlot];
    var slotUnlocked = app.selectedSlot < app.account.unlockedSlots;
    var actionLabel = !slotUnlocked ? "Locked" : (selectedArmy ? (app.pendingOverwriteSlot === app.selectedSlot ? "Confirm Overwrite" : "Overwrite") : "Save");
    return screenChrome([
      '<div class="layout">',
      '<section class="panel">',
      '<div class="panel-header"><h2>Run Summary</h2><span class="tag green">Final Won</span></div>',
      '<div class="panel-body">',
      '<div class="timeline">',
      app.run.history.map(function (entry) {
        return '<div class="timeline-row"><span class="tag">' + ns.Render.esc(entry.type) + '</span><div><strong>' + ns.Render.esc(entry.label) + '</strong><div class="muted">' + ns.Render.esc(entry.detail) + '</div></div><span class="tag gold">' + ns.Render.fmt(entry.value || 0) + '</span></div>';
      }).join(""),
      '</div>',
      '</div>',
      '</section>',
      ns.Render.armyPanel("Save This Army", candidateArmy, "Candidate is captured from the pre-final snapshot. The final proves it, but does not make the saved reward worse."),
      '<section class="panel">',
      '<div class="panel-header"><h2>Save Slot</h2><span class="tag gold">' + app.account.unlockedSlots + ' unlocked</span></div>',
      '<div class="panel-body">',
      ns.Render.slots(app.account, app.selectedSlot),
      '<div class="actions"><button class="primary" data-action="save-army"' + (!slotUnlocked ? " disabled" : "") + '>' + actionLabel + '</button><button data-action="to-saved">View Saved Armies</button></div>',
      '<p class="muted">Account Progress: +' + ns.Render.fmt(150) + ' XP on final victory. Slots are physical capacity 8, unlocked count starts at 2.</p>',
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderSaved() {
    var saved = app.account.savedArmies[app.selectedSlot];
    return screenChrome([
      '<div class="layout">',
      '<section class="panel">',
      '<div class="panel-header"><h2>Saved Armies</h2><span class="tag green">immutable snapshots</span></div>',
      '<div class="panel-body">',
      ns.Render.slots(app.account, app.selectedSlot),
      '<div class="actions"><button data-action="restart">Start New Run</button></div>',
      '</div>',
      '</section>',
      '<section class="panel">',
      '<div class="panel-header"><h2>Selected Army</h2>' + (saved ? '<span class="tag gold">' + ns.Render.fmt(ns.State.armyValue(saved.army)) + '</span>' : '<span class="tag">none</span>') + '</div>',
      '<div class="panel-body">',
      saved ? [
        '<h3>' + ns.Render.esc(saved.name) + '</h3>',
        '<p class="muted">Created from run ' + ns.Render.esc(saved.createdFromRunId) + '. Source: ' + ns.Render.esc(saved.source) + '.</p>',
        ns.Render.armyRows(saved.army),
        '<div class="actions"><button class="primary" data-action="set-defense">Set Defence</button><button data-action="simulate-offence">Simulate Offence Result</button></div>'
      ].join("") : '<div class="empty">Select a taken slot to preview the saved army.</div>',
      '</div>',
      '</section>',
      '<section class="panel">',
      '<div class="panel-header"><h2>Attack History</h2><span class="tag">review only</span></div>',
      '<div class="panel-body">',
      saved && saved.attackHistory && saved.attackHistory.length ? '<div class="log">' + saved.attackHistory.map(function (result) {
        return '<div class="log-entry"><strong>' + ns.Render.esc(result.result) + '</strong><br>Rank ' + ns.Render.fmt(result.rankBefore) + ' -> ' + ns.Render.fmt(result.rankAfter) + ' (' + result.rankDelta + ')<br>XP +' + ns.Render.fmt(result.xpGained) + '</div>';
      }).join("") + '</div>' : '<div class="empty">No attack history yet. Opponent selection is a future separate screen.</div>',
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderResult() {
    var result = app.asyncResult;
    return screenChrome([
      '<div class="screen">',
      '<section class="panel">',
      '<div class="panel-header"><h2>Async Battle Result</h2><span class="tag green">' + ns.Render.esc(result.result) + '</span></div>',
      '<div class="panel-body grid-3">',
      '<div class="card"><div class="card-head"><h3>Ranking</h3></div><div class="card-body"><p>' + ns.Render.fmt(result.rankBefore) + ' -> ' + ns.Render.fmt(result.rankAfter) + '</p><span class="tag ' + (result.rankDelta >= 0 ? "green" : "red") + '">' + (result.rankDelta >= 0 ? "+" : "") + result.rankDelta + '</span></div></div>',
      '<div class="card"><div class="card-head"><h3>Account XP</h3></div><div class="card-body"><p>XP +' + ns.Render.fmt(result.xpGained) + '</p><div class="bar"><span style="width:' + Math.min(100, Math.round(app.account.xp / app.account.xpToNext * 100)) + '%"></span></div><p class="muted">Next unlock progress</p></div></div>',
      '<div class="card"><div class="card-head"><h3>Preservation</h3></div><div class="card-body"><p>No army stolen, destroyed, or edited.</p><span class="tag green">preserved</span></div></div>',
      '</div>',
      '<div class="actions"><button class="primary" data-action="continue-saved">View Armies</button><button data-action="restart">New Run</button></div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function renderRunLost() {
    return screenChrome([
      '<div class="screen">',
      '<section class="panel">',
      '<div class="panel-header"><h2>Run Failed</h2><span class="tag red">no saved army</span></div>',
      '<div class="panel-body">',
      '<p class="muted">Failed runs do not produce saved armies. Run-only skills and changes are lost with this run.</p>',
      ns.Render.log(app.run ? app.run.history : []),
      '<div class="actions"><button class="primary" data-action="restart">Start New Run</button><button data-action="to-saved">Saved Armies</button></div>',
      '</div>',
      '</section>',
      '</div>'
    ].join(""));
  }

  function render() {
    var html;
    if (app.screen === "task20") { html = renderTask20Mockup(); }
    if (app.screen === "task21") { html = renderTask21Mockup(); }
    if (app.screen === "start") { html = renderStart(); }
    if (app.screen === "map") { html = renderMap(); }
    if (app.screen === "battle") { html = renderBattle(); }
    if (app.screen === "reward") { html = renderReward(); }
    if (app.screen === "shop") { html = renderShop(); }
    if (app.screen === "summary") { html = renderSummary(); }
    if (app.screen === "saved") { html = renderSaved(); }
    if (app.screen === "result") { html = renderResult(); }
    if (app.screen === "lost") { html = renderRunLost(); }
    root.innerHTML = html || renderStart();
  }

  function completeRewardSource() {
    if (app.activeRewards && app.activeRewards.source === "node") {
      markCurrentNodeComplete();
    }
    app.activeRewards = null;
    app.focusedRewardId = null;
    app.screen = "map";
  }

  function handleAction(action, target) {
    if (action === "noop") {
      return;
    }

    if (action === "open-task20-mockup") {
      app.screen = "task20";
      app.message = "";
      render();
      return;
    }

    if (action === "open-task21-mockup") {
      app.screen = "task21";
      app.message = "";
      render();
      return;
    }

    if (action === "open-run-prototype") {
      app.screen = app.run ? "map" : "start";
      render();
      return;
    }

    if (action === "scroll-mockup") {
      scrollToMockup(target.dataset.anchor);
      return;
    }

    if (action === "select-mockup-starting") {
      app.selectedStartingArmyId = target.dataset.id;
      app.mockupRunCreated = false;
      render();
      scrollToMockup("mockup-20-armies");
      return;
    }

    if (action === "select-mockup-route") {
      app.selectedRouteId = target.dataset.id;
      app.mockupRunCreated = false;
      render();
      scrollToMockup("mockup-20-routes");
      return;
    }

    if (action === "focus-mockup-offer") {
      app.mockupFocusedOfferId = target.dataset.id;
      render();
      scrollToMockup("mockup-21-preview");
      return;
    }

    if (action === "mockup-begin-run") {
      app.mockupRunCreated = true;
      render();
      scrollToMockup("mockup-20-created");
      return;
    }

    if (action === "select-starting") {
      app.selectedStartingArmyId = target.dataset.id;
      render();
      return;
    }

    if (action === "select-route") {
      app.selectedRouteId = target.dataset.id;
      render();
      return;
    }

    if (action === "begin-run") {
      app.run = ns.State.createRun(app.selectedStartingArmyId, app.selectedRouteId);
      app.selectedNodeId = app.run.nodes[0].id;
      app.screen = "map";
      app.message = "";
      render();
      return;
    }

    if (action === "select-node") {
      app.selectedNodeId = target.dataset.node;
      render();
      return;
    }

    if (action === "travel-node") {
      var node = getSelectedNode();
      if (!node || node.state !== "available") {
        return;
      }
      if (node.type === "battle" || node.type === "final") {
        app.activeBattle = ns.AI.createBattleContext(app.run, node);
        app.screen = "battle";
      } else if (node.type === "reward") {
        app.activeRewards = ns.Rewards.buildRewardChoice(app.run, "node");
        app.focusedRewardId = app.activeRewards.cards[0].id;
        app.screen = "reward";
      } else if (node.type === "shop") {
        app.activeShop = ns.Shop.buildShopVisit(app.run, node);
        app.focusedOfferId = app.activeShop.offers[0].id;
        app.screen = "shop";
      }
      render();
      return;
    }

    if (action === "resolve-battle") {
      var result = ns.AI.resolveBattle(app.run, app.activeBattle);
      if (result.result !== "win") {
        app.run.status = "failed";
        app.screen = "lost";
        render();
        return;
      }
      markCurrentNodeComplete();
      if (result.nodeType === "final") {
        app.run.status = "final-won";
        app.account.xp += 150;
        ns.State.saveAccount(app.account);
        app.screen = "summary";
      } else {
        app.activeRewards = ns.Rewards.buildRewardChoice(app.run, "battle");
        app.focusedRewardId = app.activeRewards.cards[0].id;
        app.screen = "reward";
      }
      render();
      return;
    }

    if (action === "focus-reward") {
      app.focusedRewardId = target.dataset.id;
      render();
      return;
    }

    if (action === "confirm-reward") {
      var card = getFocusedReward();
      if (card) {
        ns.Rewards.confirmCard(app.run, card);
      }
      completeRewardSource();
      render();
      return;
    }

    if (action === "focus-offer") {
      app.focusedOfferId = target.dataset.id;
      render();
      return;
    }

    if (action === "buy-offer") {
      var offer = getFocusedOffer();
      if (offer) {
        ns.Shop.buyOffer(app.run, offer);
      }
      render();
      return;
    }

    if (action === "leave-shop") {
      markCurrentNodeComplete();
      app.activeShop = null;
      app.focusedOfferId = null;
      app.screen = "map";
      render();
      return;
    }

    if (action === "to-map") {
      app.screen = app.run ? "map" : "start";
      render();
      return;
    }

    if (action === "select-slot") {
      app.selectedSlot = Number(target.dataset.slot);
      app.pendingOverwriteSlot = null;
      render();
      return;
    }

    if (action === "save-army") {
      if (!app.run || app.run.status !== "final-won" || app.selectedSlot >= app.account.unlockedSlots) {
        return;
      }
      var existing = app.account.savedArmies[app.selectedSlot];
      if (existing && app.pendingOverwriteSlot !== app.selectedSlot) {
        app.pendingOverwriteSlot = app.selectedSlot;
        render();
        return;
      }
      var army = app.run.preFinalSnapshot || app.run.army;
      app.account.savedArmies[app.selectedSlot] = {
        id: ns.State.nextId("saved"),
        name: app.run.routeName + " Army",
        army: ns.State.clone(army),
        createdFromRunId: app.run.id,
        source: "offline-local-saved-army-roster",
        immutable: true,
        attackHistory: []
      };
      if (app.account.currentDefenseSlot == null) {
        app.account.currentDefenseSlot = app.selectedSlot;
      }
      ns.State.saveAccount(app.account);
      app.pendingOverwriteSlot = null;
      app.screen = "saved";
      render();
      return;
    }

    if (action === "to-saved") {
      app.account = ns.State.loadAccount();
      app.screen = "saved";
      render();
      return;
    }

    if (action === "set-defense") {
      if (app.account.savedArmies[app.selectedSlot] && app.selectedSlot < app.account.unlockedSlots) {
        app.account.currentDefenseSlot = app.selectedSlot;
        ns.State.saveAccount(app.account);
      }
      render();
      return;
    }

    if (action === "simulate-offence") {
      var saved = app.account.savedArmies[app.selectedSlot];
      if (saved) {
        app.asyncResult = ns.AI.simulateAsyncResult(app.account, saved, app.selectedSlot);
        ns.State.saveAccount(app.account);
        app.screen = "result";
      }
      render();
      return;
    }

    if (action === "continue-saved") {
      app.screen = "saved";
      render();
      return;
    }

    if (action === "restart") {
      var account = ns.State.loadAccount();
      app = ns.State.createAppState();
      app.account = account;
      render();
    }
  }

  function init(initialState, element) {
    app = initialState;
    root = element;
    root.addEventListener("click", function (event) {
      var target = event.target.closest("[data-action]");
      if (!target) {
        return;
      }
      event.preventDefault();
      handleAction(target.dataset.action, target);
    });
    render();
  }

  ns.UI = {
    init: init,
    render: render
  };
})(window.Retsot = window.Retsot || {});

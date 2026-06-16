(function (ns) {
  "use strict";

  function boot() {
    ns.UI.init(ns.State.createAppState(), document.getElementById("app"));
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})(window.Retsot = window.Retsot || {});

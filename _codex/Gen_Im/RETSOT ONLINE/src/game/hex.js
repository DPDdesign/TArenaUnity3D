(function (ns) {
  "use strict";

  var directions = [
    { q: 1, r: 0 },
    { q: 1, r: -1 },
    { q: 0, r: -1 },
    { q: -1, r: 0 },
    { q: -1, r: 1 },
    { q: 0, r: 1 }
  ];

  function distance(a, b) {
    var dq = a.q - b.q;
    var dr = a.r - b.r;
    var ds = (-a.q - a.r) - (-b.q - b.r);
    return (Math.abs(dq) + Math.abs(dr) + Math.abs(ds)) / 2;
  }

  function demoCells() {
    var cells = [];
    for (var r = 0; r < 5; r += 1) {
      for (var q = 0; q < 7; q += 1) {
        cells.push({ q: q, r: r, type: q < 2 ? "player" : (q > 4 ? "enemy" : "open") });
      }
    }
    return cells;
  }

  ns.Hex = {
    directions: directions,
    distance: distance,
    demoCells: demoCells
  };
})(window.Retsot = window.Retsot || {});

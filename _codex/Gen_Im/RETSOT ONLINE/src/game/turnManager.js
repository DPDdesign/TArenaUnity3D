(function (ns) {
  "use strict";

  function pickTeamCandidate(team) {
    var ready = team
      .filter(function (stack) { return stack.amount > 0 && !stack.moved && !stack.waited && !stack.blinded; })
      .sort(function (a, b) {
        return ns.UNITS[b.unitId].initiative - ns.UNITS[a.unitId].initiative;
      });

    if (ready.length > 0) {
      return ready[0];
    }

    var waited = team
      .filter(function (stack) { return stack.amount > 0 && !stack.moved && !stack.blinded; })
      .sort(function (a, b) {
        return ns.UNITS[a.unitId].initiative - ns.UNITS[b.unitId].initiative;
      });

    return waited[0] || null;
  }

  function compareCandidates(red, blue) {
    if (!red) {
      return blue;
    }
    if (!blue) {
      return red;
    }

    var redUnit = ns.UNITS[red.unitId];
    var blueUnit = ns.UNITS[blue.unitId];

    if (red.waited !== blue.waited) {
      return red.waited ? blue : red;
    }

    if (!red.waited) {
      if (redUnit.initiative !== blueUnit.initiative) {
        return redUnit.initiative > blueUnit.initiative ? red : blue;
      }
      if (redUnit.speed !== blueUnit.speed) {
        return redUnit.speed > blueUnit.speed ? red : blue;
      }
      return red;
    }

    if (redUnit.initiative !== blueUnit.initiative) {
      return redUnit.initiative < blueUnit.initiative ? red : blue;
    }
    if (redUnit.speed !== blueUnit.speed) {
      return redUnit.speed < blueUnit.speed ? red : blue;
    }
    return red;
  }

  function pickActiveUnit(redTeam, blueTeam) {
    return compareCandidates(pickTeamCandidate(redTeam), pickTeamCandidate(blueTeam));
  }

  ns.TurnManager = {
    pickTeamCandidate: pickTeamCandidate,
    pickActiveUnit: pickActiveUnit
  };
})(window.Retsot = window.Retsot || {});

using TimeSpells;
using UnityEngine;

public static class BattleActionAutomaticResultApplier
{
    public static BattleActionResult CreateTrapTriggeredResult(TosterHexUnit target, HexClass hex)
    {
        BattleActionResult result = new BattleActionResult
        {
            ActionKind = BattleActionKind.Trap,
            ActorUnitId = UnitLabel(target)
        };

        if (target == null || hex == null || hex.isTraped == false || hex.trap == null)
        {
            return result;
        }

        string trapId = hex.trap.NameOfTraps ?? string.Empty;
        TosterHexUnit owner = hex.trap.TosterWhoSetupThisTrap;
        if (trapId != "Fire_Trap" || target != owner)
        {
            result.Add(new BattleActionResultEvent
            {
                EventType = BattleActionResultEventType.TrapTriggered,
                ActorUnitId = UnitLabel(owner),
                TargetUnitId = UnitLabel(target),
                Hex = new HexCoord(hex.C, hex.R),
                TrapId = trapId,
                Message = target.Name + " wszedl w " + trapId
            });
        }

        if (trapId == "Rope_Trap")
        {
            result.Add(new BattleActionResultEvent
            {
                EventType = BattleActionResultEventType.StatusApplied,
                ActorUnitId = UnitLabel(owner),
                TargetUnitId = UnitLabel(target),
                Hex = new HexCoord(hex.C, hex.R),
                StatusId = "Rope_Trap",
                Duration = 1,
                SpecialResistanceModifier = 30,
                RemoveTrap = true
            });
        }
        else if (trapId == "Fire_Trap")
        {
            if (target != owner)
            {
                int damage;
                if (TryResolveTrapDamage(owner, target, trapId, out damage) == false)
                {
                    return result;
                }

                result.Add(new BattleActionResultEvent
                {
                    EventType = BattleActionResultEventType.StatusApplied,
                    ActorUnitId = UnitLabel(owner),
                    TargetUnitId = UnitLabel(target),
                    Hex = new HexCoord(hex.C, hex.R),
                    StatusId = "Fire_Trap",
                    Duration = 5,
                    DamageOverTime = damage / 5
                });
            }
        }
        else if (trapId == "Spike_Trap")
        {
            int damage;
            if (TryResolveTrapDamage(owner, target, trapId, out damage) == false)
            {
                return result;
            }

            result.Add(new BattleActionResultEvent
            {
                EventType = BattleActionResultEventType.StatusApplied,
                ActorUnitId = UnitLabel(owner),
                TargetUnitId = UnitLabel(target),
                Hex = new HexCoord(hex.C, hex.R),
                StatusId = "Spike_Trap",
                Duration = 2,
                MovementModifier = -2,
                DamageOverTime = damage,
                RemoveTrap = true,
                TrimPathSteps = 2
            });
        }

        return result;
    }

    public static BattleActionResult CreateFireMovementTrapResult(TosterHexUnit actor, HexClass hex)
    {
        BattleActionResult result = new BattleActionResult
        {
            ActionKind = BattleActionKind.Automatic,
            ActorUnitId = UnitLabel(actor)
        };

        if (actor == null || hex == null)
        {
            return result;
        }

        result.Add(new BattleActionResultEvent
        {
            EventType = BattleActionResultEventType.TrapPlaced,
            ActorUnitId = UnitLabel(actor),
            Hex = new HexCoord(hex.C, hex.R),
            TrapId = "Fire_Trap",
            Duration = 2,
            ShowTrapImmediately = false,
            PresentationSkillId = "Fire_Movement"
        });

        return result;
    }

    public static BattleActionResult CreateTrapTurnTickResult(HexClass hex)
    {
        BattleActionResult result = new BattleActionResult
        {
            ActionKind = BattleActionKind.Automatic
        };

        if (hex == null || hex.isTraped == false || hex.trap == null)
        {
            return result;
        }

        result.Add(new BattleActionResultEvent
        {
            EventType = BattleActionResultEventType.TrapExpired,
            Hex = new HexCoord(hex.C, hex.R),
            TrapId = hex.trap.NameOfTraps ?? string.Empty,
            Amount = -1,
            RemoveTrap = hex.trap.Time <= 1
        });

        return result;
    }

    public static BattleActionResult CreateStatusTickResult(TosterHexUnit target, SpellOverTime spell)
    {
        BattleActionResult result = new BattleActionResult
        {
            ActionKind = BattleActionKind.Passive,
            ActorUnitId = UnitLabel(target)
        };

        if (target == null || spell == null)
        {
            return result;
        }

        result.Add(new BattleActionResultEvent
        {
            EventType = BattleActionResultEventType.PassiveTriggered,
            ActorUnitId = UnitLabel(spell.SourceUnit),
            TargetUnitId = UnitLabel(target),
            StatusId = spell.nameofspell ?? string.Empty,
            DamageOverTime = spell.DamageOverTime,
            Amount = -1
        });

        return result;
    }

    public static BattleActionResult CreateAutocastStatusResult(TosterHexUnit actor, string skillId)
    {
        return CreateStatusApplicationResult(
            actor,
            actor,
            skillId,
            1,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            BattleActionKind.Automatic);
    }

    public static BattleActionResult CreateStatusApplicationResult(
        TosterHexUnit source,
        TosterHexUnit target,
        string statusId,
        int duration,
        int hpModifier,
        int attackModifier,
        int defenseModifier,
        int movementModifier,
        int initiativeModifier,
        int maxDamageModifier,
        int minDamageModifier,
        int damageOverTime,
        int resistanceModifier,
        int counterAttacksModifier,
        int damageModifier,
        int specialResistanceModifier,
        bool isStackable,
        BattleActionKind actionKind)
    {
        BattleActionResult result = new BattleActionResult
        {
            ActionKind = actionKind,
            ActorUnitId = UnitLabel(source)
        };

        if (target == null || string.IsNullOrEmpty(statusId))
        {
            return result;
        }

        result.Add(new BattleActionResultEvent
        {
            EventType = BattleActionResultEventType.StatusApplied,
            ActorUnitId = UnitLabel(source),
            TargetUnitId = UnitLabel(target),
            StatusId = statusId,
            Duration = duration,
            HpModifier = hpModifier,
            AttackModifier = attackModifier,
            DefenseModifier = defenseModifier,
            MovementModifier = movementModifier,
            InitiativeModifier = initiativeModifier,
            MaxDamageModifier = maxDamageModifier,
            MinDamageModifier = minDamageModifier,
            DamageOverTime = damageOverTime,
            ResistanceModifier = resistanceModifier,
            CounterAttacksModifier = counterAttacksModifier,
            DamageModifier = damageModifier,
            SpecialResistanceModifier = specialResistanceModifier,
            IsStackable = isStackable
        });

        return result;
    }

    public static void ApplyTrapResult(TosterHexUnit target, HexClass hex, BattleActionResult result)
    {
        if (target == null || hex == null || result == null || result.Events == null)
        {
            return;
        }

        TosterHexUnit trapOwner = hex.trap != null ? hex.trap.TosterWhoSetupThisTrap : null;
        for (int i = 0; i < result.Events.Count; i++)
        {
            BattleActionResultEvent resultEvent = result.Events[i];
            if (resultEvent == null)
            {
                continue;
            }

            if (resultEvent.EventType == BattleActionResultEventType.TrapTriggered)
            {
                target.SendTrapTriggeredMsg(resultEvent.TrapId, trapOwner);
            }
            else if (resultEvent.EventType == BattleActionResultEventType.StatusApplied)
            {
                target.AddNewTimeSpell(
                    resultEvent.Duration,
                    trapOwner,
                    resultEvent.HpModifier,
                    resultEvent.AttackModifier,
                    resultEvent.DefenseModifier,
                    resultEvent.MovementModifier,
                    resultEvent.InitiativeModifier,
                    resultEvent.MaxDamageModifier,
                    resultEvent.MinDamageModifier,
                    resultEvent.DamageOverTime,
                    resultEvent.ResistanceModifier,
                    resultEvent.CounterAttacksModifier,
                    resultEvent.DamageModifier,
                    resultEvent.SpecialResistanceModifier,
                    resultEvent.StatusId,
                    resultEvent.IsStackable);

                if (resultEvent.TrimPathSteps > 0)
                {
                    target.TrimHexPathTail(resultEvent.TrimPathSteps);
                }
            }

            if (resultEvent.RemoveTrap && hex.isTraped)
            {
                hex.RemoveTrap();
            }
        }
    }

    public static void ApplyFireMovementTrapResult(TosterHexUnit actor, HexClass hex, BattleActionResult result)
    {
        if (actor == null || hex == null || result == null || result.Events == null)
        {
            return;
        }

        for (int i = 0; i < result.Events.Count; i++)
        {
            BattleActionResultEvent resultEvent = result.Events[i];
            if (resultEvent == null || resultEvent.EventType != BattleActionResultEventType.TrapPlaced)
            {
                continue;
            }

            hex.AddTrap(resultEvent.TrapId, resultEvent.Duration, actor, resultEvent.ShowTrapImmediately, resultEvent.PresentationSkillId);
            SkillPresentationManager.PlaySequencedHexEffect(resultEvent.PresentationSkillId, actor, hex, null);
            if (hex.hexMap != null)
            {
                hex.hexMap.StartCoroutine(TosterHexUnit.RevealFireTrapAfterDelay(hex, TosterHexUnit.FireMovementTrapRevealDelaySeconds));
            }
        }
    }

    public static void ApplyTrapTurnTickResult(HexClass hex, BattleActionResult result)
    {
        if (hex == null || hex.isTraped == false || hex.trap == null || result == null || result.Events == null)
        {
            return;
        }

        for (int i = 0; i < result.Events.Count; i++)
        {
            BattleActionResultEvent resultEvent = result.Events[i];
            if (resultEvent == null || resultEvent.EventType != BattleActionResultEventType.TrapExpired)
            {
                continue;
            }

            hex.trap.Time += resultEvent.Amount;
            if (resultEvent.RemoveTrap || hex.trap.Time <= 0)
            {
                hex.RemoveTrap();
            }
        }
    }

    public static void ApplyStatusTickResult(TosterHexUnit target, SpellOverTime spell, BattleActionResult result)
    {
        if (target == null || spell == null || result == null || result.Events == null)
        {
            return;
        }

        for (int i = 0; i < result.Events.Count; i++)
        {
            BattleActionResultEvent resultEvent = result.Events[i];
            if (resultEvent == null || resultEvent.EventType != BattleActionResultEventType.PassiveTriggered)
            {
                continue;
            }

            spell.DoTurn();
            if (spell.IsOver())
            {
                target.SpellsGoingOn.Remove(spell);
            }
        }
    }

    public static void ApplyAutocastStatusResult(TosterHexUnit actor, BattleActionResult result)
    {
        ApplyStatusApplicationResult(actor, actor, result);
    }

    public static void ApplyStatusApplicationResult(TosterHexUnit source, TosterHexUnit target, BattleActionResult result)
    {
        if (target == null || result == null || result.Events == null)
        {
            return;
        }

        for (int i = 0; i < result.Events.Count; i++)
        {
            BattleActionResultEvent resultEvent = result.Events[i];
            if (resultEvent == null || resultEvent.EventType != BattleActionResultEventType.StatusApplied)
            {
                continue;
            }

            target.AddNewTimeSpell(
                resultEvent.Duration,
                source,
                resultEvent.HpModifier,
                resultEvent.AttackModifier,
                resultEvent.DefenseModifier,
                resultEvent.MovementModifier,
                resultEvent.InitiativeModifier,
                resultEvent.MaxDamageModifier,
                resultEvent.MinDamageModifier,
                resultEvent.DamageOverTime,
                resultEvent.ResistanceModifier,
                resultEvent.CounterAttacksModifier,
                resultEvent.DamageModifier,
                resultEvent.SpecialResistanceModifier,
                resultEvent.StatusId,
                resultEvent.IsStackable);
        }
    }

    static string UnitLabel(TosterHexUnit unit)
    {
        return unit != null ? unit.Name ?? string.Empty : string.Empty;
    }

    static bool TryResolveTrapDamage(TosterHexUnit owner, TosterHexUnit target, string trapId, out int damage)
    {
        string error;
        if (LiveCombatDamageResolver.TryCalculateCommittedDamage(
            owner,
            target,
            "trap:" + (trapId ?? string.Empty),
            1.0,
            out damage,
            out error))
        {
            return true;
        }

        Debug.LogError("[CombatDamage] trap damage failed for " + (trapId ?? "<null>") + ": " + error);
        return false;
    }
}

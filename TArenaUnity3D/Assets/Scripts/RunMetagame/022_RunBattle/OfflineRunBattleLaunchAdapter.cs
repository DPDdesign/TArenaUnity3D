using System;

public class OfflineRunBattleLaunchAdapter : IRunBattleLaunchAdapter
{
    public RunBattleLaunchRecord CreateLaunchRecord(RunBattleLaunchPayload payload)
    {
        if (payload == null)
        {
            return null;
        }

        return new RunBattleLaunchRecord(
            "legacy-launch-" + Guid.NewGuid().ToString("N"),
            payload.RunBattleId,
            "legacy-player-army-adapter:" + payload.CurrentArmySnapshotId,
            "legacy-enemy-army-adapter:" + payload.EnemyArmySourceId,
            "current HexMap/TeamClass build-spawn path; PlayerPrefs/local files are adapter surfaces only",
            "offline-local-run-battle-launch-adapter");
    }
}

using System;
using System.Data;

public static class OfflineDatabaseAccountBootstrap
{
    public const int DefaultAccountId = 1;
    public const string SmartCastPreferenceKey = "combat.smart_cast_enabled";
    public const string AnimationSpeedPreferenceKey = "combat.animation_speed_multiplier";
    public const float DefaultAnimationSpeedPreferenceValue = 1f;
    private const string DefaultDisplayName = "Offline Account";
    private static readonly string[] StartRunUnitUnlocks =
    {
        "Axeman",
        "FireElemental",
        "FleshGolem",
        "Healer",
        "HeavyHitter",
        "Rusher",
        "Specialist",
        "StoneGolem",
        "Tank",
        "Trapper",
        "Thrower",
        "Wisp"
    };
    private static readonly string[] StartRunSkillUnlocks =
    {
        "Axe_Rain",
        "Blind_by_light",
        "Chope",
        "Cold_Blood",
        "Defence_Ritual",
        "Double_Throw",
        "Fire_Ball",
        "Fire_Movement",
        "Fire_Skin",
        "Force_Pull",
        "Hate",
        "Heavy_Fists",
        "Insult",
        "Long_Lick",
        "Massochism",
        "Melee_Stance_Barb",
        "Melee_Stance_Lizard",
        "Rage",
        "Range_Stance_Barb",
        "Range_Stance_Lizard",
        "Rope_Trap",
        "Rotting",
        "Rush",
        "Shapeshift",
        "Slash",
        "Spike_Trap",
        "Stone_Skin",
        "Stone_Stance",
        "Stone_Throw",
        "Terrifying_Presence",
        "Tough_Skin",
        "Toxic_Fume",
        "Unstoppable_Light"
    };

    public static int EnsureDefaultAccount(IDbConnection connection, IDbTransaction transaction, string externalAccountId)
    {
        object existing = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT account_id FROM offline_accounts WHERE account_id = @accountId LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", DefaultAccountId));

        if (existing != null && existing != DBNull.Value)
        {
            EnsureStartRunUnlocks(connection, transaction, DefaultAccountId);
            EnsureDefaultPreferences(connection, transaction, DefaultAccountId);
            return DefaultAccountId;
        }

        string now = OfflineDatabaseSql.UtcNowText();
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO offline_accounts (
    account_id,
    external_account_id,
    display_name,
    created_at_utc,
    updated_at_utc,
    unlocked_saved_army_slots,
    is_active
) VALUES (
    @accountId,
    @externalAccountId,
    @displayName,
    @createdAtUtc,
    @updatedAtUtc,
    @unlockedSavedArmySlots,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", DefaultAccountId),
            new OfflineDatabaseSqlParameter("@externalAccountId", string.IsNullOrEmpty(externalAccountId) ? "offline-player" : externalAccountId),
            new OfflineDatabaseSqlParameter("@displayName", DefaultDisplayName),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now),
            new OfflineDatabaseSqlParameter("@unlockedSavedArmySlots", 2));

        EnsureStartRunUnlocks(connection, transaction, DefaultAccountId);
        EnsureDefaultPreferences(connection, transaction, DefaultAccountId);
        return DefaultAccountId;
    }

    private static void EnsureStartRunUnlocks(IDbConnection connection, IDbTransaction transaction, int accountId)
    {
        for (int i = 0; i < StartRunUnitUnlocks.Length; i++)
        {
            UpsertUnlock(connection, transaction, accountId, DBUnlockTypeId.Unit, StartRunUnitUnlocks[i]);
        }

        for (int i = 0; i < StartRunSkillUnlocks.Length; i++)
        {
            UpsertUnlock(connection, transaction, accountId, DBUnlockTypeId.Skill, StartRunSkillUnlocks[i]);
        }
    }

    private static void UpsertUnlock(
        IDbConnection connection,
        IDbTransaction transaction,
        int accountId,
        DBUnlockTypeId unlockTypeId,
        string targetId)
    {
        object existing = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT unlock_id
FROM account_unlocks
WHERE account_id = @accountId
  AND unlock_type_id = @unlockTypeId
  AND target_id = @targetId
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@unlockTypeId", (int)unlockTypeId),
            new OfflineDatabaseSqlParameter("@targetId", targetId));

        if (existing != null && existing != DBNull.Value)
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE account_unlocks
SET is_active = 1
WHERE unlock_id = @unlockId;",
                transaction,
                new OfflineDatabaseSqlParameter("@unlockId", OfflineDatabaseSql.ReadInt(existing)));
            return;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO account_unlocks (
    account_id,
    unlock_type_id,
    target_id,
    unlocked_at_utc,
    is_active
) VALUES (
    @accountId,
    @unlockTypeId,
    @targetId,
    @unlockedAtUtc,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@unlockTypeId", (int)unlockTypeId),
            new OfflineDatabaseSqlParameter("@targetId", targetId),
            new OfflineDatabaseSqlParameter("@unlockedAtUtc", OfflineDatabaseSql.UtcNowText()));
    }

    private static void EnsureDefaultPreferences(IDbConnection connection, IDbTransaction transaction, int accountId)
    {
        EnsureBoolPreference(connection, transaction, accountId, SmartCastPreferenceKey, false);
        EnsureFloatPreference(connection, transaction, accountId, AnimationSpeedPreferenceKey, DefaultAnimationSpeedPreferenceValue);
    }

    private static void EnsureBoolPreference(
        IDbConnection connection,
        IDbTransaction transaction,
        int accountId,
        string preferenceKey,
        bool defaultValue)
    {
        object existing = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT preference_key
FROM player_preferences
WHERE account_id = @accountId
  AND preference_key = @preferenceKey
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@preferenceKey", preferenceKey));

        if (existing != null && existing != DBNull.Value)
        {
            return;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO player_preferences (
    account_id,
    preference_key,
    bool_value,
    float_value,
    updated_at_utc
) VALUES (
    @accountId,
    @preferenceKey,
    @boolValue,
    0,
    @updatedAtUtc
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@preferenceKey", preferenceKey),
            new OfflineDatabaseSqlParameter("@boolValue", defaultValue ? 1 : 0),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()));
    }

    private static void EnsureFloatPreference(
        IDbConnection connection,
        IDbTransaction transaction,
        int accountId,
        string preferenceKey,
        float defaultValue)
    {
        object existing = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT preference_key
FROM player_preferences
WHERE account_id = @accountId
  AND preference_key = @preferenceKey
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@preferenceKey", preferenceKey));

        if (existing != null && existing != DBNull.Value)
        {
            return;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO player_preferences (
    account_id,
    preference_key,
    bool_value,
    float_value,
    updated_at_utc
) VALUES (
    @accountId,
    @preferenceKey,
    0,
    @floatValue,
    @updatedAtUtc
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@preferenceKey", preferenceKey),
            new OfflineDatabaseSqlParameter("@floatValue", defaultValue),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()));
    }
}

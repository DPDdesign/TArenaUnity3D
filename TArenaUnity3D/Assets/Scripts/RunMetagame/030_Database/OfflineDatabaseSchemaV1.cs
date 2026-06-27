using System.Collections.Generic;

public static class OfflineDatabaseSchemaV1
{
    public const int Version = 1;

    public static List<string> BuildStatements()
    {
        return new List<string>
        {
            TableSchemaVersion(),
            TableOfflineAccounts(),
            TablePlayerPreferences(),
            TableAccountUnlocks(),
            TableOfflineRuns(),
            TableArmySnapshots(),
            TableArmySnapshotStacks(),
            TableArmySnapshotStackSkills(),
            TableMapNodes(),
            TableMapNodeConnections(),
            TableRewardOpportunities(),
            IndexRewardOpportunitiesRunNodeSlot(),
            TableMapNodeRewards(),
            TableMapNodeEnemies(),
            TableRunEvents(),
            TableRunBattles(),
            TableRunBattleLosses(),
            TableRewardChoices(),
            TableRewardCards(),
            TableShopVisits(),
            TableShopOffers(),
            TableShopPurchases(),
            TableRunSummaries(),
            TableRunSummaryEntries(),
            TableSavedArmySlots(),
            TableSavedArmies(),
            TableSavedArmyRosterState(),
            TableSavedArmyHistory(),
            TableAsyncBattleResults()
        };
    }

    private static string TableSchemaVersion()
    {
        return @"
CREATE TABLE IF NOT EXISTS schema_version (
    id INTEGER PRIMARY KEY,
    version INTEGER NOT NULL,
    applied_at_utc TEXT NOT NULL,
    notes TEXT
);";
    }

    private static string TableOfflineAccounts()
    {
        return @"
CREATE TABLE IF NOT EXISTS offline_accounts (
    account_id INTEGER PRIMARY KEY,
    external_account_id TEXT,
    display_name TEXT,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL,
    account_xp INTEGER NOT NULL DEFAULT 0,
    rank_value INTEGER NOT NULL DEFAULT 1000,
    unlocked_saved_army_slots INTEGER NOT NULL DEFAULT 2,
    is_active INTEGER NOT NULL DEFAULT 1
);";
    }

    private static string TableAccountUnlocks()
    {
        return @"
CREATE TABLE IF NOT EXISTS account_unlocks (
    unlock_id INTEGER PRIMARY KEY,
    account_id INTEGER NOT NULL,
    unlock_type_id INTEGER NOT NULL,
    target_id TEXT NOT NULL,
    unlocked_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (account_id) REFERENCES offline_accounts(account_id)
);";
    }

    private static string TablePlayerPreferences()
    {
        return @"
CREATE TABLE IF NOT EXISTS player_preferences (
    account_id INTEGER NOT NULL,
    preference_key TEXT NOT NULL,
    bool_value INTEGER NOT NULL DEFAULT 0,
    float_value REAL NOT NULL DEFAULT 0,
    updated_at_utc TEXT NOT NULL,
    PRIMARY KEY (account_id, preference_key),
    FOREIGN KEY (account_id) REFERENCES offline_accounts(account_id)
);";
    }

    private static string TableOfflineRuns()
    {
        return @"
CREATE TABLE IF NOT EXISTS offline_runs (
    run_id INTEGER PRIMARY KEY,
    account_id INTEGER NOT NULL,
    game_mode_id INTEGER NOT NULL,
    authority_source_id INTEGER NOT NULL,
    run_status_id INTEGER NOT NULL,
    starting_army_template_id TEXT NOT NULL,
    starting_army_variant_id TEXT NOT NULL,
    selected_starting_army_id TEXT NOT NULL,
    selected_route_choice_id TEXT NOT NULL,
    route_map_id INTEGER,
    current_node_id INTEGER,
    current_army_snapshot_id INTEGER,
    start_army_snapshot_id INTEGER,
    pre_final_army_snapshot_id INTEGER,
    current_run_gold INTEGER NOT NULL DEFAULT 0,
    stage_progress INTEGER NOT NULL DEFAULT 0,
    route_progress INTEGER NOT NULL DEFAULT 0,
    run_seed INTEGER NOT NULL DEFAULT 35035,
    run_seed_version INTEGER NOT NULL DEFAULT 1,
    next_screen TEXT,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (account_id) REFERENCES offline_accounts(account_id)
);";
    }

    private static string TableMapNodes()
    {
        return @"
CREATE TABLE IF NOT EXISTS map_nodes (
    node_id INTEGER PRIMARY KEY,
    run_id INTEGER NOT NULL,
    route_map_id INTEGER,
    route_path_id INTEGER,
    catalog_entry_id TEXT,
    catalog_path_id TEXT,
    node_type_id INTEGER NOT NULL,
    node_state_id INTEGER NOT NULL,
    stage_index INTEGER NOT NULL DEFAULT 0,
    display_name TEXT,
    possible_reward_hint TEXT,
    expected_risk_hint TEXT,
    encounter_id TEXT,
    completed_at_utc TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id)
);";
    }

    private static string TableMapNodeConnections()
    {
        return @"
CREATE TABLE IF NOT EXISTS map_node_connections (
    connection_id INTEGER PRIMARY KEY,
    run_id INTEGER NOT NULL,
    from_node_id INTEGER NOT NULL,
    to_node_id INTEGER NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id),
    FOREIGN KEY (from_node_id) REFERENCES map_nodes(node_id),
    FOREIGN KEY (to_node_id) REFERENCES map_nodes(node_id)
);";
    }

    private static string TableRewardOpportunities()
    {
        return @"
CREATE TABLE IF NOT EXISTS reward_opportunities (
    reward_opportunity_id INTEGER PRIMARY KEY,
    run_id INTEGER NOT NULL,
    node_id INTEGER NOT NULL,
    reward_slot_index INTEGER NOT NULL,
    planned_operation_type TEXT NOT NULL,
    catalog_entry_id TEXT NOT NULL,
    run_seed INTEGER NOT NULL DEFAULT 35035,
    seed_version INTEGER NOT NULL DEFAULT 1,
    opportunity_state_id INTEGER NOT NULL DEFAULT 1,
    reward_choice_id INTEGER NOT NULL DEFAULT 0,
    resolved_reward_card_id INTEGER,
    resolved_card_reward_id TEXT NOT NULL DEFAULT '',
    created_at_utc TEXT NOT NULL,
    resolved_at_utc TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id),
    FOREIGN KEY (node_id) REFERENCES map_nodes(node_id)
);";
    }

    private static string IndexRewardOpportunitiesRunNodeSlot()
    {
        return @"
CREATE UNIQUE INDEX IF NOT EXISTS idx_reward_opportunities_run_node_slot_active
ON reward_opportunities(run_id, node_id, reward_slot_index, is_active);";
    }

    private static string TableMapNodeRewards()
    {
        return @"
CREATE TABLE IF NOT EXISTS map_node_rewards (
    reward_id INTEGER PRIMARY KEY,
    node_id INTEGER NOT NULL,
    reward_choice_id INTEGER NOT NULL DEFAULT 0,
    reward_slot_index INTEGER NOT NULL,
    card_reward_id TEXT NOT NULL DEFAULT '',
    catalog_entry_id TEXT NOT NULL,
    base_snapshot_id INTEGER NOT NULL,
    target_snapshot_stack_id INTEGER,
    reward_type TEXT NOT NULL,
    unit_id TEXT,
    to_unit_id TEXT,
    amount INTEGER NOT NULL DEFAULT 0,
    currency_delta INTEGER NOT NULL DEFAULT 0,
    operation_json TEXT NOT NULL,
    legal INTEGER NOT NULL DEFAULT 1,
    error_id INTEGER NOT NULL DEFAULT 0,
    is_selected INTEGER NOT NULL DEFAULT 0,
    applied_snapshot_id INTEGER,
    is_fallback INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (node_id) REFERENCES map_nodes(node_id),
    FOREIGN KEY (base_snapshot_id) REFERENCES army_snapshots(snapshot_id),
    FOREIGN KEY (target_snapshot_stack_id) REFERENCES army_snapshot_stacks(snapshot_stack_id),
    FOREIGN KEY (applied_snapshot_id) REFERENCES army_snapshots(snapshot_id)
);";
    }

    private static string TableMapNodeEnemies()
    {
        return @"
CREATE TABLE IF NOT EXISTS map_node_enemies (
    enemy_id INTEGER PRIMARY KEY,
    node_id INTEGER NOT NULL,
    catalog_entry_id TEXT,
    army_snapshot_id INTEGER,
    encounter_id TEXT,
    enemy_rule_id TEXT,
    risk_band TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (node_id) REFERENCES map_nodes(node_id),
    FOREIGN KEY (army_snapshot_id) REFERENCES army_snapshots(snapshot_id)
);";
    }

    private static string TableArmySnapshots()
    {
        return @"
CREATE TABLE IF NOT EXISTS army_snapshots (
    snapshot_id INTEGER PRIMARY KEY,
    account_id INTEGER NOT NULL,
    run_id INTEGER,
    saved_army_id INTEGER,
    node_id INTEGER,
    created_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (account_id) REFERENCES offline_accounts(account_id),
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id)
);";
    }

    private static string TableArmySnapshotStacks()
    {
        return @"
CREATE TABLE IF NOT EXISTS army_snapshot_stacks (
    snapshot_stack_id INTEGER PRIMARY KEY,
    snapshot_id INTEGER NOT NULL,
    unit_id TEXT NOT NULL,
    amount INTEGER NOT NULL DEFAULT 0,
    formation_slot INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (snapshot_id) REFERENCES army_snapshots(snapshot_id)
);";
    }

    private static string TableArmySnapshotStackSkills()
    {
        return @"
CREATE TABLE IF NOT EXISTS army_snapshot_stack_skills (
    snapshot_stack_skill_id INTEGER PRIMARY KEY,
    snapshot_stack_id INTEGER NOT NULL,
    skill_id TEXT NOT NULL,
    acquired_at_run_node_id INTEGER,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (snapshot_stack_id) REFERENCES army_snapshot_stacks(snapshot_stack_id)
);";
    }

    private static string TableRunEvents()
    {
        return @"
CREATE TABLE IF NOT EXISTS run_events (
    event_id INTEGER PRIMARY KEY,
    run_id INTEGER NOT NULL,
    account_id INTEGER NOT NULL,
    node_id INTEGER,
    event_type_id INTEGER NOT NULL,
    before_snapshot_id INTEGER,
    after_snapshot_id INTEGER,
    run_gold_before INTEGER,
    run_gold_after INTEGER,
    result TEXT,
    created_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id),
    FOREIGN KEY (account_id) REFERENCES offline_accounts(account_id),
    FOREIGN KEY (node_id) REFERENCES map_nodes(node_id)
);";
    }

    private static string TableRunBattles()
    {
        return @"
CREATE TABLE IF NOT EXISTS run_battles (
    run_battle_id INTEGER PRIMARY KEY,
    event_id INTEGER NOT NULL,
    run_id INTEGER NOT NULL,
    node_id INTEGER NOT NULL,
    encounter_id TEXT NOT NULL,
    enemy_goal TEXT,
    battle_status_id INTEGER NOT NULL,
    pre_battle_snapshot_id INTEGER,
    post_battle_snapshot_id INTEGER,
    launch_payload_json TEXT,
    launch_adapter_surface TEXT,
    battle_outcome_id INTEGER,
    result_source TEXT,
    next_screen TEXT,
    prepared_at_utc TEXT,
    completed_at_utc TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (event_id) REFERENCES run_events(event_id),
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id),
    FOREIGN KEY (node_id) REFERENCES map_nodes(node_id)
);";
    }

    private static string TableRunBattleLosses()
    {
        return @"
CREATE TABLE IF NOT EXISTS run_battle_losses (
    loss_id INTEGER PRIMARY KEY,
    run_battle_id INTEGER NOT NULL,
    snapshot_stack_id INTEGER NOT NULL,
    unit_id TEXT NOT NULL,
    amount_before INTEGER NOT NULL,
    amount_after INTEGER NOT NULL,
    lost_amount INTEGER NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (run_battle_id) REFERENCES run_battles(run_battle_id),
    FOREIGN KEY (snapshot_stack_id) REFERENCES army_snapshot_stacks(snapshot_stack_id)
);";
    }

    private static string TableRewardChoices()
    {
        return @"
CREATE TABLE IF NOT EXISTS reward_choices (
    reward_choice_id INTEGER PRIMARY KEY,
    event_id INTEGER NOT NULL,
    run_id INTEGER NOT NULL,
    run_battle_id INTEGER,
    node_id INTEGER,
    army_before_reward_snapshot_id INTEGER NOT NULL,
    focused_reward_id TEXT,
    focused_reward_slot_index INTEGER NOT NULL DEFAULT -1,
    selected_reward_id TEXT,
    selected_reward_slot_index INTEGER NOT NULL DEFAULT -1,
    run_gold_before INTEGER NOT NULL DEFAULT 0,
    run_gold_after INTEGER,
    choice_status_id INTEGER NOT NULL,
    created_at_utc TEXT NOT NULL,
    applied_at_utc TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (event_id) REFERENCES run_events(event_id),
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id),
    FOREIGN KEY (run_battle_id) REFERENCES run_battles(run_battle_id),
    FOREIGN KEY (node_id) REFERENCES map_nodes(node_id)
);";
    }

    private static string TableRewardCards()
    {
        return @"
CREATE TABLE IF NOT EXISTS reward_cards (
    reward_card_id INTEGER PRIMARY KEY,
    reward_choice_id INTEGER NOT NULL,
    reward_id TEXT NOT NULL DEFAULT '',
    reward_slot_index INTEGER NOT NULL DEFAULT 0,
    template_id TEXT NOT NULL,
    family_id INTEGER NOT NULL,
    intention_id INTEGER NOT NULL,
    rarity_id INTEGER,
    title_id TEXT,
    verb_id TEXT,
    affected_stack_id TEXT,
    affected_slot_index INTEGER NOT NULL DEFAULT -1,
    target_snapshot_stack_id INTEGER,
    operation_type TEXT NOT NULL DEFAULT '',
    operation_json TEXT NOT NULL,
    legal INTEGER NOT NULL DEFAULT 1,
    error_id INTEGER NOT NULL DEFAULT 0,
    preview_text_before TEXT,
    preview_text_after TEXT,
    preview_snapshot_id INTEGER,
    applied_snapshot_id INTEGER,
    is_selected INTEGER NOT NULL DEFAULT 0,
    is_fallback INTEGER NOT NULL DEFAULT 0,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (reward_choice_id) REFERENCES reward_choices(reward_choice_id),
    FOREIGN KEY (target_snapshot_stack_id) REFERENCES army_snapshot_stacks(snapshot_stack_id)
);";
    }

    private static string TableShopVisits()
    {
        return @"
CREATE TABLE IF NOT EXISTS shop_visits (
    shop_visit_id INTEGER PRIMARY KEY,
    run_id INTEGER NOT NULL,
    node_id INTEGER NOT NULL,
    visit_status_id INTEGER NOT NULL,
    army_before_shop_snapshot_id INTEGER NOT NULL,
    current_army_snapshot_id INTEGER NOT NULL,
    run_gold_before INTEGER NOT NULL DEFAULT 0,
    current_run_gold INTEGER NOT NULL DEFAULT 0,
    focused_offer_id TEXT,
    created_at_utc TEXT NOT NULL,
    left_at_utc TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id),
    FOREIGN KEY (node_id) REFERENCES map_nodes(node_id)
);";
    }

    private static string TableShopOffers()
    {
        return @"
CREATE TABLE IF NOT EXISTS shop_offers (
    shop_offer_id INTEGER PRIMARY KEY,
    shop_visit_id INTEGER NOT NULL,
    offer_id TEXT NOT NULL,
    offer_category_id INTEGER NOT NULL,
    title_id TEXT,
    detail_id TEXT,
    cost INTEGER NOT NULL DEFAULT 0,
    available INTEGER NOT NULL DEFAULT 1,
    purchased INTEGER NOT NULL DEFAULT 0,
    affected_snapshot_stack_id INTEGER,
    operation_json TEXT NOT NULL,
    preview_text_before TEXT,
    preview_text_after TEXT,
    preview_snapshot_id INTEGER,
    purchase_snapshot_id INTEGER,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (shop_visit_id) REFERENCES shop_visits(shop_visit_id),
    FOREIGN KEY (affected_snapshot_stack_id) REFERENCES army_snapshot_stacks(snapshot_stack_id)
);";
    }

    private static string TableShopPurchases()
    {
        return @"
CREATE TABLE IF NOT EXISTS shop_purchases (
    shop_purchase_id INTEGER PRIMARY KEY,
    event_id INTEGER NOT NULL,
    shop_visit_id INTEGER NOT NULL,
    shop_offer_id INTEGER NOT NULL,
    run_id INTEGER NOT NULL,
    run_gold_before INTEGER NOT NULL,
    run_gold_after INTEGER NOT NULL,
    army_before_purchase_snapshot_id INTEGER NOT NULL,
    army_after_purchase_snapshot_id INTEGER NOT NULL,
    purchase_result_id INTEGER NOT NULL,
    message TEXT,
    purchased_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (event_id) REFERENCES run_events(event_id),
    FOREIGN KEY (shop_visit_id) REFERENCES shop_visits(shop_visit_id),
    FOREIGN KEY (shop_offer_id) REFERENCES shop_offers(shop_offer_id),
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id)
);";
    }

    private static string TableRunSummaries()
    {
        return @"
CREATE TABLE IF NOT EXISTS run_summaries (
    run_summary_id INTEGER PRIMARY KEY,
    run_id INTEGER NOT NULL,
    final_result_id INTEGER NOT NULL,
    start_snapshot_id INTEGER,
    pre_final_snapshot_id INTEGER,
    post_final_snapshot_id INTEGER,
    saved_army_candidate_snapshot_id INTEGER,
    account_xp_awarded INTEGER NOT NULL DEFAULT 0,
    next_unlock_preview TEXT,
    created_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (run_id) REFERENCES offline_runs(run_id)
);";
    }

    private static string TableRunSummaryEntries()
    {
        return @"
CREATE TABLE IF NOT EXISTS run_summary_entries (
    summary_entry_id INTEGER PRIMARY KEY,
    run_summary_id INTEGER NOT NULL,
    entry_type_id INTEGER NOT NULL,
    title_id TEXT,
    detail_id TEXT,
    run_gold_delta INTEGER NOT NULL DEFAULT 0,
    snapshot_id INTEGER,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (run_summary_id) REFERENCES run_summaries(run_summary_id),
    FOREIGN KEY (snapshot_id) REFERENCES army_snapshots(snapshot_id)
);";
    }

    private static string TableSavedArmySlots()
    {
        return @"
CREATE TABLE IF NOT EXISTS saved_army_slots (
    slot_id INTEGER PRIMARY KEY,
    account_id INTEGER NOT NULL,
    slot_index INTEGER NOT NULL,
    saved_army_id INTEGER,
    locked INTEGER NOT NULL DEFAULT 0,
    updated_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (account_id) REFERENCES offline_accounts(account_id)
);";
    }

    private static string TableSavedArmies()
    {
        return @"
CREATE TABLE IF NOT EXISTS saved_armies (
    saved_army_id INTEGER PRIMARY KEY,
    account_id INTEGER NOT NULL,
    snapshot_id INTEGER NOT NULL,
    created_from_run_id INTEGER,
    active INTEGER NOT NULL DEFAULT 1,
    replaced_by_saved_army_id INTEGER,
    created_at_utc TEXT NOT NULL,
    deactivated_at_utc TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (account_id) REFERENCES offline_accounts(account_id),
    FOREIGN KEY (snapshot_id) REFERENCES army_snapshots(snapshot_id),
    FOREIGN KEY (created_from_run_id) REFERENCES offline_runs(run_id),
    FOREIGN KEY (replaced_by_saved_army_id) REFERENCES saved_armies(saved_army_id)
);";
    }

    private static string TableSavedArmyRosterState()
    {
        return @"
CREATE TABLE IF NOT EXISTS saved_army_roster_state (
    account_id INTEGER PRIMARY KEY,
    current_defence_saved_army_id INTEGER,
    updated_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (account_id) REFERENCES offline_accounts(account_id),
    FOREIGN KEY (current_defence_saved_army_id) REFERENCES saved_armies(saved_army_id)
);";
    }

    private static string TableSavedArmyHistory()
    {
        return @"
CREATE TABLE IF NOT EXISTS saved_army_history (
    history_id INTEGER PRIMARY KEY,
    saved_army_id INTEGER NOT NULL,
    async_battle_result_id INTEGER,
    result_kind_id INTEGER NOT NULL,
    opponent_name TEXT,
    attacker_value_at_battle INTEGER NOT NULL DEFAULT 0,
    defender_value_at_battle INTEGER NOT NULL DEFAULT 0,
    rank_delta INTEGER NOT NULL DEFAULT 0,
    recorded_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (saved_army_id) REFERENCES saved_armies(saved_army_id)
);";
    }

    private static string TableAsyncBattleResults()
    {
        return @"
CREATE TABLE IF NOT EXISTS async_battle_results (
    async_battle_result_id INTEGER PRIMARY KEY,
    account_id INTEGER NOT NULL,
    attacker_saved_army_id INTEGER NOT NULL,
    defender_saved_army_id INTEGER NOT NULL,
    opponent_id TEXT,
    opponent_name TEXT,
    result_kind_id INTEGER NOT NULL,
    rank_before INTEGER NOT NULL,
    rank_after INTEGER NOT NULL,
    rank_delta INTEGER NOT NULL,
    account_xp_before INTEGER NOT NULL,
    account_xp_after INTEGER NOT NULL,
    account_xp_gained INTEGER NOT NULL,
    next_unlock_preview TEXT,
    preservation_record TEXT,
    result_source TEXT NOT NULL,
    recorded_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (account_id) REFERENCES offline_accounts(account_id),
    FOREIGN KEY (attacker_saved_army_id) REFERENCES saved_armies(saved_army_id),
    FOREIGN KEY (defender_saved_army_id) REFERENCES saved_armies(saved_army_id)
);";
    }
}

using System.Text.Json;
using Microsoft.Data.Sqlite;
using Dapper;

namespace SkyAuto.Infrastructure.Storage;

public static class SqliteDbInitializer
{
    public static void RunMigrations(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        conn.Execute(@"
CREATE TABLE IF NOT EXISTS workflows (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    enabled INTEGER DEFAULT 1,
    variables TEXT,
    steps TEXT,
    last_run_time TEXT,
    last_result TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS assets (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    type TEXT DEFAULT 'image',
    file_path TEXT,
    description TEXT,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS schedules (
    id TEXT PRIMARY KEY,
    workflow_id TEXT NOT NULL,
    rule_type TEXT DEFAULT 'daily',
    cron_expression TEXT,
    interval_minutes INTEGER DEFAULT 60,
    enabled INTEGER DEFAULT 1,
    next_run_time TEXT,
    last_result TEXT,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS run_records (
    id TEXT PRIMARY KEY,
    workflow_id TEXT NOT NULL,
    workflow_name TEXT,
    start_time TEXT NOT NULL,
    end_time TEXT,
    success INTEGER DEFAULT 0,
    failed_step_name TEXT,
    error_message TEXT,
    screenshot_path TEXT,
    step_records_json TEXT
);

CREATE TABLE IF NOT EXISTS run_step_records (
    id TEXT PRIMARY KEY,
    run_record_id TEXT NOT NULL,
    [index] INTEGER DEFAULT 0,
    action_type TEXT,
    success INTEGER DEFAULT 1,
    start_time TEXT,
    end_time TEXT,
    duration_ms INTEGER DEFAULT 0,
    output_data TEXT,
    error_message TEXT,
    screenshot_path TEXT
);

CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    value TEXT
);

CREATE TABLE IF NOT EXISTS self_check_records (
    id TEXT PRIMARY KEY,
    name TEXT,
    category TEXT,
    status TEXT DEFAULT 'pending',
    evidence TEXT,
    checked_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ola_function_status (
    function_key TEXT PRIMARY KEY,
    category TEXT NOT NULL,
    chinese_name TEXT NOT NULL,
    raw_function_name TEXT,
    parameters_json TEXT,
    implemented INTEGER NOT NULL DEFAULT 0,
    real_ola_connected INTEGER NOT NULL DEFAULT 0,
    tested INTEGER NOT NULL DEFAULT 0,
    test_status TEXT NOT NULL DEFAULT 'pending',
    test_message TEXT,
    last_tested_at TEXT
);

CREATE TABLE IF NOT EXISTS runtime_locks (
    lock_key TEXT PRIMARY KEY,
    owner_run_id TEXT,
    acquired_at TEXT,
    expires_at TEXT
);

CREATE TABLE IF NOT EXISTS workflow_variables (
    id TEXT PRIMARY KEY,
    workflow_id TEXT,
    name TEXT NOT NULL,
    key TEXT NOT NULL,
    type TEXT NOT NULL,
    scope TEXT NOT NULL,
    default_value TEXT,
    required INTEGER DEFAULT 0,
    is_secret INTEGER DEFAULT 0,
    description TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS variable_values (
    id TEXT PRIMARY KEY,
    variable_id TEXT NOT NULL,
    workflow_id TEXT,
    run_id TEXT,
    step_id TEXT,
    value TEXT,
    is_encrypted INTEGER DEFAULT 0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);");

        conn.Execute("CREATE INDEX IF NOT EXISTS idx_run_records_workflow_id ON run_records(workflow_id)");
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_schedules_workflow_id ON schedules(workflow_id)");
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_run_step_records_run_id ON run_step_records(run_record_id)");
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_workflow_variables_workflow_id ON workflow_variables(workflow_id)");
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_workflow_variables_key ON workflow_variables(key)");
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_variable_values_variable_id ON variable_values(variable_id)");
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_variable_values_workflow_id ON variable_values(workflow_id)");
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_variable_values_run_id ON variable_values(run_id)");
    }
}

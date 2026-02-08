-- Enable UUID extension if not enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Users Table
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) UNIQUE NOT NULL,
    email_verified_at TIMESTAMP WITH TIME ZONE,
    password_hash VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Magic Tokens Table
CREATE TABLE IF NOT EXISTS auth_magic_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    used_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User Sessions Table
CREATE TABLE IF NOT EXISTS user_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    session_token_hash VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Projects Table
CREATE TABLE IF NOT EXISTS projects (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, slug)
);

-- Project API Keys Table
CREATE TABLE IF NOT EXISTS project_api_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    key_type SMALLINT NOT NULL,       -- 0: Ping, 1: FullAccess, 2: ReadAccess
    key_hash TEXT NOT NULL,           -- hashed full key
    key_prefix TEXT NOT NULL,         -- first 8 chars for display
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    revoked_at TIMESTAMP WITH TIME ZONE
);

-- Monitors Table
CREATE TABLE IF NOT EXISTS monitors (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL,
    schedule_type SMALLINT NOT NULL DEFAULT 0, -- 0: Interval, 1: Cron, 2: Calendar
    period_seconds INTEGER,
    cron_expression VARCHAR(255),
    cron_timezone VARCHAR(100),
    calendar_expression VARCHAR(255),
    calendar_timezone VARCHAR(100),
    grace_seconds INTEGER DEFAULT 0,
    last_ping_at TIMESTAMP WITH TIME ZONE,
    last_status SMALLINT, -- 0: Failed, 1: Success, 2: Paused
    next_expected_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE,
    UNIQUE(project_id, slug)
);

-- Monitor Pings Table
CREATE TABLE IF NOT EXISTS monitor_pings (
    id SERIAL PRIMARY KEY,
    monitor_id UUID NOT NULL REFERENCES monitors(id) ON DELETE CASCADE,
    received_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    status SMALLINT NOT NULL, -- 0: Success, 1: Late, 2: Down
    message TEXT,
    ip_address INET,
    user_agent TEXT,
    http_method VARCHAR(10),
    request_headers JSONB,
    duration_ms INTEGER
);

-- Integrations Table (Project-level)
CREATE TABLE IF NOT EXISTS integrations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    type SMALLINT NOT NULL,   -- 1=Slack, 2=Teams, 3=Email, 4=PagerDuty
    name TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_integrations_project_id ON integrations(project_id);

-- Slack Integrations Table
CREATE TABLE IF NOT EXISTS slack_integrations (
    integration_id UUID PRIMARY KEY REFERENCES integrations(id) ON DELETE CASCADE,
    workspace_id TEXT NOT NULL,
    channel_id TEXT NOT NULL,
    channel_name TEXT NOT NULL,
    encrypted_bot_token TEXT NOT NULL,
    workspace_name TEXT NOT NULL,
    app_id TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Monitor Integrations Mapping Table
CREATE TABLE IF NOT EXISTS monitor_integrations (
    monitor_id UUID NOT NULL REFERENCES monitors(id) ON DELETE CASCADE,
    integration_id UUID NOT NULL REFERENCES integrations(id) ON DELETE CASCADE,
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    PRIMARY KEY (monitor_id, integration_id)
);

-- Notification Jobs Table
CREATE TABLE IF NOT EXISTS notification_jobs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    monitor_ping_id INTEGER NOT NULL,
    integration_id UUID NOT NULL REFERENCES integrations(id) ON DELETE CASCADE,
    alert_type SMALLINT NOT NULL, -- 1=Down, 2=Recovery
    status SMALLINT NOT NULL DEFAULT 0, -- 0=Pending, 1=Processing, 2=Sent, 3=Failed
    retry_count SMALLINT NOT NULL DEFAULT 0,
    last_error TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_notification_ping_integration ON notification_jobs(monitor_ping_id, integration_id);
CREATE INDEX IF NOT EXISTS idx_notification_jobs_status ON notification_jobs(status);
CREATE INDEX IF NOT EXISTS idx_notification_jobs_integration_id ON notification_jobs(integration_id);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_sessions_token_hash ON user_sessions(session_token_hash);
CREATE INDEX IF NOT EXISTS idx_magic_tokens_hash ON auth_magic_tokens(token_hash);
CREATE INDEX IF NOT EXISTS idx_projects_user_id ON projects(user_id);
CREATE INDEX IF NOT EXISTS idx_monitors_project_id ON monitors(project_id);
CREATE INDEX IF NOT EXISTS idx_monitor_pings_monitor_id ON monitor_pings(monitor_id);


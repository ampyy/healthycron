-- Migration: Add Telegram, Pushover, Spike.sh integration tables

-- Telegram
CREATE TABLE IF NOT EXISTS telegram_integrations (
    integration_id UUID PRIMARY KEY,
    chat_id TEXT NOT NULL,
    chat_name TEXT,
    bot_username TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_telegram_integrations
        FOREIGN KEY (integration_id)
        REFERENCES integrations (id)
        ON DELETE CASCADE
);

-- Pushover
CREATE TABLE IF NOT EXISTS pushover_integrations (
    integration_id UUID PRIMARY KEY,
    user_key TEXT NOT NULL,
    device TEXT,
    priority SMALLINT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_pushover_integrations
        FOREIGN KEY (integration_id)
        REFERENCES integrations (id)
        ON DELETE CASCADE
);

-- Spike.sh
CREATE TABLE IF NOT EXISTS spike_integrations (
    integration_id UUID PRIMARY KEY,
    webhook_url TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_spike_integrations
        FOREIGN KEY (integration_id)
        REFERENCES integrations (id)
        ON DELETE CASCADE
);

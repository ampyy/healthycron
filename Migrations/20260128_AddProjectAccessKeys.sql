-- Migration to add project_api_keys table
DROP TABLE IF EXISTS project_api_keys CASCADE;

CREATE TABLE project_api_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    key_type SMALLINT NOT NULL,       -- 0: Ping, 1: FullAccess, 2: ReadAccess
    key_hash TEXT NOT NULL,           -- hashed full key
    key_prefix TEXT NOT NULL,         -- first 8 chars for display
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    revoked_at TIMESTAMP WITH TIME ZONE
);

-- Add missing columns to projects if they don't exist
DO $$ 
BEGIN 
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='projects' AND column_name='color') THEN
        ALTER TABLE projects ADD COLUMN color VARCHAR(50) DEFAULT 'blue';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='projects' AND column_name='icon') THEN
        ALTER TABLE projects ADD COLUMN icon VARCHAR(50) DEFAULT 'folder';
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_project_api_keys_project_id ON project_api_keys(project_id);
CREATE INDEX IF NOT EXISTS idx_project_api_keys_hash ON project_api_keys(key_hash);

CREATE TABLE agents (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    ip_address VARCHAR(45) NOT NULL,
    status VARCHAR(50) NOT NULL,
    last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE vulnerabilities (
    id SERIAL PRIMARY KEY,
    agent_id INT REFERENCES agents(id),
    cve_id VARCHAR(50) NOT NULL,
    description TEXT,
    severity VARCHAR(50),
    discovered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE antivirus_signatures (
    id SERIAL PRIMARY KEY,
    signature_hash VARCHAR(255) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE patch_management (
    id SERIAL PRIMARY KEY,
    agent_id INT REFERENCES agents(id),
    patch_name VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL,
    applied_at TIMESTAMP
);

CREATE TABLE logs (
    id SERIAL PRIMARY KEY,
    agent_id INT REFERENCES agents(id),
    action VARCHAR(255) NOT NULL,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    details TEXT
);
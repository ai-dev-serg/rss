-- SQL Script to Create RSS Items Table

CREATE TABLE IF NOT EXISTS rss_items (
    id SERIAL PRIMARY KEY,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    link VARCHAR(1000) NOT NULL,
    pub_date TIMESTAMP NOT NULL,
    author VARCHAR(200),
    category VARCHAR(100)
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_rss_items_pub_date ON rss_items(pub_date);
CREATE INDEX IF NOT EXISTS idx_rss_items_title ON rss_items(title);
CREATE INDEX IF NOT EXISTS idx_rss_items_category ON rss_items(category);
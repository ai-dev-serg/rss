-- SQL Script to Create RSS Items Table
CREATE TABLE IF NOT EXISTS rss_items (
    id SERIAL PRIMARY KEY,
    title VARCHAR(1000) NOT NULL,
    description TEXT,
    link VARCHAR(2000) NOT NULL,
    pub_date TIMESTAMP WITH TIME ZONE NOT NULL,
    author VARCHAR(500),
    category VARCHAR(250)
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_rss_items_pub_date ON rss_items(pub_date);
CREATE INDEX IF NOT EXISTS idx_rss_items_title ON rss_items(title);
CREATE INDEX IF NOT EXISTS idx_rss_items_category ON rss_items(category);
CREATE INDEX IF NOT EXISTS idx_rss_items_author ON rss_items(author);
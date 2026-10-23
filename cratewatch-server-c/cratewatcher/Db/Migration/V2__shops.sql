-- A webshop a User wants scanned. The template holds {query}, replaced by "artist title" when scanning.
CREATE TABLE shops (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    search_url_template VARCHAR(1000) NOT NULL
);

CREATE INDEX idx_shops_user ON shops(user_id);

-- Last scan result: this shop shows a product link for this release.
CREATE TABLE shop_availabilities (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    shop_id UUID NOT NULL REFERENCES shops(id) ON DELETE CASCADE,
    cd_release_id UUID NOT NULL REFERENCES cd_releases(id) ON DELETE CASCADE,
    url VARCHAR(2000) NOT NULL,
    checked_at TIMESTAMPTZ NOT NULL,
    UNIQUE (shop_id, cd_release_id)
);

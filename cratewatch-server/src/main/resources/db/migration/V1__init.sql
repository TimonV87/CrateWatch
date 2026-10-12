CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    username VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL
);

CREATE UNIQUE INDEX idx_users_username ON users(username);

CREATE TABLE artists (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    name VARCHAR(255) NOT NULL,
    musicbrainz_id VARCHAR(36) NOT NULL UNIQUE
);

CREATE TABLE cd_releases (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    artist_id UUID NOT NULL REFERENCES artists(id) ON DELETE CASCADE,
    title VARCHAR(500) NOT NULL,
    release_type VARCHAR(30) NOT NULL DEFAULT 'OTHER',
    first_release_date DATE,
    musicbrainz_release_group_id VARCHAR(36),
    cover_art_url VARCHAR(1000)
);

CREATE UNIQUE INDEX idx_cd_releases_artist_title ON cd_releases(artist_id, title);

-- Added after cd_releases so the artist -> cover-release pointer can reference it.
-- Deleting the referenced release just clears the pointer, not the artist.
ALTER TABLE artists ADD COLUMN cover_release_id UUID REFERENCES cd_releases(id) ON DELETE SET NULL;

CREATE TABLE shop_links (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    cd_release_id UUID NOT NULL REFERENCES cd_releases(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    url VARCHAR(2000) NOT NULL,
    label VARCHAR(255)
);

CREATE INDEX idx_shop_links_cd_release ON shop_links(cd_release_id);
CREATE INDEX idx_shop_links_user ON shop_links(user_id);

-- Which Users track which Artists (shared catalog, not a private copy per User).
CREATE TABLE user_artists (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    artist_id UUID NOT NULL REFERENCES artists(id) ON DELETE CASCADE,
    UNIQUE (user_id, artist_id)
);

CREATE INDEX idx_user_artists_user ON user_artists(user_id);

-- Per-User Wishlist/Owned/etc state for a shared Release.
CREATE TABLE user_release_states (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    cd_release_id UUID NOT NULL REFERENCES cd_releases(id) ON DELETE CASCADE,
    state VARCHAR(20) NOT NULL DEFAULT 'NONE'
        CHECK (state IN ('NONE', 'WISHLIST', 'OWNED', 'DONT_WANT')),
    UNIQUE (user_id, cd_release_id)
);

CREATE INDEX idx_user_release_states_user ON user_release_states(user_id);

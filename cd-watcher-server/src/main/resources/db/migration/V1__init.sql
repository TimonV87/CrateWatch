CREATE TABLE app_user (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    username VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL
);

CREATE UNIQUE INDEX idx_app_user_username ON app_user(username);

CREATE TABLE artist (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    user_id UUID NOT NULL REFERENCES app_user(id),
    name VARCHAR(255) NOT NULL,
    musicbrainz_id VARCHAR(36)
);

CREATE INDEX idx_artist_user ON artist(user_id);

CREATE TABLE cd_release (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    artist_id UUID NOT NULL REFERENCES artist(id),
    title VARCHAR(500) NOT NULL,
    release_type VARCHAR(30) NOT NULL DEFAULT 'OTHER',
    state VARCHAR(20) NOT NULL DEFAULT 'NONE'
        CHECK (state IN ('NONE', 'WISHLIST', 'OWNED', 'DONT_WANT')),
    musicbrainz_release_group_id VARCHAR(36),
    cover_art_url VARCHAR(1000)
);

CREATE UNIQUE INDEX idx_cd_release_artist_title ON cd_release(artist_id, title);
CREATE INDEX idx_cd_release_state ON cd_release(artist_id, state);

CREATE TABLE shop_link (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    cd_release_id UUID NOT NULL REFERENCES cd_release(id),
    url VARCHAR(2000) NOT NULL,
    label VARCHAR(255)
);

CREATE INDEX idx_shop_link_cd_release ON shop_link(cd_release_id);

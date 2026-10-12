package com.cratewatch.artist.domain;

import io.micronaut.serde.annotation.Serdeable;

@Serdeable
public record ArtistSearchResult(String musicbrainzId, String name, String disambiguation, String country) {
}

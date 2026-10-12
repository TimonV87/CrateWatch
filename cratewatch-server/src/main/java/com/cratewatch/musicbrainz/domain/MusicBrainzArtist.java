package com.cratewatch.musicbrainz.domain;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import io.micronaut.serde.annotation.Serdeable;

@Serdeable
@JsonIgnoreProperties(ignoreUnknown = true)
public record MusicBrainzArtist(
    String id,
    String name,
    String disambiguation,
    String country
) {
}

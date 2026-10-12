package com.cratewatch.musicbrainz.domain;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import io.micronaut.serde.annotation.Serdeable;

import java.util.List;

@Serdeable
@JsonIgnoreProperties(ignoreUnknown = true)
public record MusicBrainzArtistSearchResponse(List<MusicBrainzArtist> artists) {
}

package com.cratewatch.musicbrainz.domain;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import io.micronaut.serde.annotation.Serdeable;

import java.time.LocalDate;
import java.util.List;

@Serdeable
@JsonIgnoreProperties(ignoreUnknown = true)
public record MusicBrainzReleaseGroup(
    String id,
    String title,

    @JsonProperty("primary-type")
    String primaryType,

    @JsonProperty("secondary-types")
    List<String> secondaryTypes,
    
    @JsonProperty("first-release-date")
    String firstReleaseDate
) {
}

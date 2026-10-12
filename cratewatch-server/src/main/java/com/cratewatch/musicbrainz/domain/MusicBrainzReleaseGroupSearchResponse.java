package com.cratewatch.musicbrainz.domain;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import io.micronaut.serde.annotation.Serdeable;

import java.util.List;

@Serdeable
@JsonIgnoreProperties(ignoreUnknown = true)
public record MusicBrainzReleaseGroupSearchResponse(
    @JsonProperty("release-groups")
    List<MusicBrainzReleaseGroup> releaseGroups
) {
}

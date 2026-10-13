package com.cratewatch.release.domain;

import io.micronaut.serde.annotation.Serdeable;

import java.time.LocalDate;
import java.util.UUID;

@Serdeable
public record ReleaseResponse(UUID id, String title, String releaseType, String state, String coverArtUrl, LocalDate firstReleaseDate) {

    public static ReleaseResponse from(CdRelease release, String state) {
        return new ReleaseResponse(
            release.getId(),
            release.getTitle(),
            release.getReleaseType(),
            state,
            "/releases/" + release.getId() + "/cover-art",
            release.getFirstReleaseDate()
        );
    }
}

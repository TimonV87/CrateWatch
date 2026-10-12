package com.cratewatch.artist.domain;

import io.micronaut.serde.annotation.Serdeable;
import jakarta.validation.constraints.NotBlank;

@Serdeable
public record CreateArtistRequest(
    @NotBlank String name,
    @NotBlank String musicbrainzId
) {
}

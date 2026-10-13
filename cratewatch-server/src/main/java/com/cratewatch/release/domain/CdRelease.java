package com.cratewatch.release.domain;

import io.micronaut.core.annotation.Nullable;
import io.micronaut.data.annotation.GeneratedValue;
import io.micronaut.data.annotation.Id;
import io.micronaut.data.annotation.MappedEntity;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDate;
import java.util.UUID;

@Getter
@Setter
@Builder
@MappedEntity("cd_releases")
public class CdRelease {

    @Id
    @GeneratedValue(GeneratedValue.Type.UUID)
    private UUID id;

    private UUID artistId;

    private String title;

    private String releaseType;

    @Nullable
    private LocalDate firstReleaseDate;

    private String musicbrainzReleaseGroupId;

    private String coverArtUrl;
}

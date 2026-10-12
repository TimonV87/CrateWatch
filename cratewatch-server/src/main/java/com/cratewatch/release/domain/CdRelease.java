package com.cratewatch.release.domain;

import io.micronaut.data.annotation.GeneratedValue;
import io.micronaut.data.annotation.Id;
import io.micronaut.data.annotation.MappedEntity;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDate;
import java.util.UUID;

@Getter
@Setter
@MappedEntity("cd_releases")
public class CdRelease {

    @Id
    @GeneratedValue
    private UUID id;

    private UUID artistId;

    private String title;

    private String releaseType;

    private LocalDate firstReleaseDate;

    private String musicbrainzReleaseGroupId;

    private String coverArtUrl;

    public CdRelease() {
    }

    public CdRelease(
      UUID artistId,
      String title,
      String releaseType,
      LocalDate firstReleaseDate,
      String musicbrainzReleaseGroupId,
      String coverArtUrl
    ) {
        this.artistId = artistId;
        this.title = title;
        this.releaseType = releaseType;
        this.firstReleaseDate = firstReleaseDate;
        this.musicbrainzReleaseGroupId = musicbrainzReleaseGroupId;
        this.coverArtUrl = coverArtUrl;
    }
}

package com.cratewatch.artist.domain;

import io.micronaut.data.annotation.GeneratedValue;
import io.micronaut.data.annotation.Id;
import io.micronaut.data.annotation.MappedEntity;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Getter
@Setter
@MappedEntity("artists")
public class Artist {

    @Id
    @GeneratedValue
    private UUID id;

    private String name;

    private String musicbrainzId;

    private UUID coverReleaseId;

    public Artist() {
    }

    public Artist(String name, String musicbrainzId) {
        this.name = name;
        this.musicbrainzId = musicbrainzId;
    }
}

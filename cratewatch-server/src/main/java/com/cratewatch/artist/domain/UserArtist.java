package com.cratewatch.artist.domain;

import io.micronaut.data.annotation.GeneratedValue;
import io.micronaut.data.annotation.Id;
import io.micronaut.data.annotation.MappedEntity;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Getter
@Setter
@MappedEntity("user_artists")
public class UserArtist {

    @Id
    @GeneratedValue
    private UUID id;

    private UUID userId;

    private UUID artistId;

    public UserArtist() {
    }

    public UserArtist(UUID userId, UUID artistId) {
        this.userId = userId;
        this.artistId = artistId;
    }
}

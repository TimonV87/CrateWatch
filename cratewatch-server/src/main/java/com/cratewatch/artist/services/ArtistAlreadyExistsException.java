package com.cratewatch.artist.services;

import com.cratewatch.artist.domain.Artist;
import io.micronaut.serde.annotation.Serdeable;
import lombok.Getter;

@Getter
public class ArtistAlreadyExistsException extends RuntimeException {

    @Serdeable.Serializable
    private final Artist existingArtist;

    public ArtistAlreadyExistsException(Artist existingArtist) {
        super("Artist already added: " + existingArtist.getId());
        this.existingArtist = existingArtist;
    }
}

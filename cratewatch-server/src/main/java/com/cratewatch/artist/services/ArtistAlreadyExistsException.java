package com.cratewatch.artist.services;

import com.cratewatch.artist.domain.Artist;
import lombok.Getter;

@Getter
public class ArtistAlreadyExistsException extends RuntimeException {

    private final Artist existingArtist;

    public ArtistAlreadyExistsException(Artist existingArtist) {
        super("Artist already added: " + existingArtist.getId());
        this.existingArtist = existingArtist;
    }
}

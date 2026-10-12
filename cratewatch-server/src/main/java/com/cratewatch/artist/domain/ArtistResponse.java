package com.cratewatch.artist.domain;

import io.micronaut.serde.annotation.Serdeable;

import java.util.UUID;

@Serdeable
public record ArtistResponse(UUID id, String name, String musicbrainzId, String coverArtUrl) {

    public static ArtistResponse from(Artist artist) {
        return new ArtistResponse(artist.getId(), artist.getName(), artist.getMusicbrainzId(),
            "/artists/" + artist.getId() + "/photo");
    }
}

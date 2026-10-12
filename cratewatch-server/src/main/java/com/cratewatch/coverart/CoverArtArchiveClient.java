package com.cratewatch.coverart;

import io.micronaut.http.annotation.Get;
import io.micronaut.http.client.annotation.Client;

@Client(id = "coverartarchive")
public interface CoverArtArchiveClient {

    @Get("release-group/{mbid}/front-250")
    byte[] fetchFrontCover(String mbid);
}

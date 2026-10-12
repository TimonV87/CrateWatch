package com.cratewatch.theaudiodb;

import com.cratewatch.theaudiodb.domain.TheAudioDbLookupResponse;
import io.micronaut.http.annotation.Get;
import io.micronaut.http.client.annotation.Client;

@Client(id = "theaudiodb")
public interface TheAudioDbClient {

    @Get("artist-mb.php?i={mbid}")
    TheAudioDbLookupResponse lookupByMbid(String mbid);
}

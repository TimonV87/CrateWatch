package com.cratewatch.musicbrainz;

import com.cratewatch.musicbrainz.domain.MusicBrainzArtistSearchResponse;
import com.cratewatch.musicbrainz.domain.MusicBrainzReleaseGroupSearchResponse;
import io.micronaut.http.annotation.Get;
import io.micronaut.http.annotation.Header;
import io.micronaut.http.client.annotation.Client;

@Client(id = "musicbrainz")
@Header(name = "User-Agent", value = "CrateWatch/0.1 ( https://github.com/tv/cratewatch )")
public interface MusicBrainzClient {

    @Get("artist/?query={query}&limit={limit}&fmt=json")
    MusicBrainzArtistSearchResponse searchArtists(String query, int limit);

    @Get("release-group?artist={artistMbid}&limit={limit}&offset={offset}&fmt=json")
    MusicBrainzReleaseGroupSearchResponse browseReleaseGroups(String artistMbid, int limit, int offset);
}

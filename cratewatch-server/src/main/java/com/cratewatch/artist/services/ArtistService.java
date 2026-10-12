package com.cratewatch.artist.services;

import com.cratewatch.artist.domain.Artist;
import com.cratewatch.artist.domain.ArtistSearchResult;
import com.cratewatch.artist.domain.CreateArtistRequest;
import com.cratewatch.artist.domain.UserArtist;
import com.cratewatch.artist.repositories.ArtistRepository;
import com.cratewatch.artist.repositories.UserArtistRepository;
import com.cratewatch.musicbrainz.MusicBrainzClient;
import com.cratewatch.musicbrainz.domain.MusicBrainzArtistSearchResponse;
import com.cratewatch.user.domain.User;
import io.micronaut.http.client.exceptions.HttpClientResponseException;
import jakarta.inject.Singleton;
import lombok.AllArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.List;

@AllArgsConstructor
@Singleton
public class ArtistService {

    private static final Logger LOG = LoggerFactory.getLogger(ArtistService.class);

    private final MusicBrainzClient musicBrainzClient;
    private final ArtistRepository artistRepository;
    private final UserArtistRepository userArtistRepository;
    private final ArtistImportService artistImportService;

    public Artist createArtist(CreateArtistRequest request, User user) {
        Artist artist = artistRepository.findByMusicbrainzId(request.musicbrainzId())
            .orElseGet(() -> importNewArtist(request));

        if (userArtistRepository.existsByUserIdAndArtistId(user.getId(), artist.getId())) {
            throw new ArtistAlreadyExistsException(artist);
        }

        userArtistRepository.save(new UserArtist(user.getId(), artist.getId()));
        return artist;
    }

    private Artist importNewArtist(CreateArtistRequest request) {
        Artist artist = artistRepository.save(new Artist(request.name(), request.musicbrainzId()));
        try {
            return artistImportService.importReleases(artist);
        } catch (HttpClientResponseException e) {
            LOG.error("Error creating artist and importing albums: {}", e.getMessage(), e);
            artistRepository.delete(artist);
            throw e;
        }
    }

    public List<ArtistSearchResult> searchAlbums(String artistName) {
        MusicBrainzArtistSearchResponse response;
        try {
            response = musicBrainzClient.searchArtists(artistName, 15);
        } catch (HttpClientResponseException e) {
            LOG.error("MusicBrainz search failed: status={} body={}", e.getStatus(), e.getResponse().getBody(String.class).orElse(""), e);
            throw e;
        }
        if (response == null) {
            LOG.error("MusicBrainz search returned a null response body for query '{}'", artistName);
            return List.of();
        }

        return response.artists()
            .stream()
            .map(a -> new ArtistSearchResult(a.id(), a.name(), a.disambiguation(), a.country()))
            .toList();
    }
}

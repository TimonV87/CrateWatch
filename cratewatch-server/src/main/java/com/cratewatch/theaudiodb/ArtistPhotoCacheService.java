package com.cratewatch.theaudiodb;

import com.cratewatch.artist.domain.Artist;
import com.cratewatch.theaudiodb.domain.TheAudioDbArtist;
import com.cratewatch.theaudiodb.domain.TheAudioDbLookupResponse;
import io.micronaut.context.annotation.Value;
import jakarta.inject.Singleton;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;
import java.util.Optional;

@Singleton
public class ArtistPhotoCacheService {

    private static final Logger LOG = LoggerFactory.getLogger(ArtistPhotoCacheService.class);

    private final TheAudioDbClient theAudioDbClient;
    private final String storagePath;
    private final HttpClient httpClient = HttpClient.newHttpClient();

    public ArtistPhotoCacheService(TheAudioDbClient theAudioDbClient,
                                    @Value("${cratewatch.artist-photo.storage-path}") String storagePath) {
        this.theAudioDbClient = theAudioDbClient;
        this.storagePath = storagePath;
    }

    public Optional<byte[]> getPhoto(Artist artist) {
        if (artist.getMusicbrainzId() == null) {
            return Optional.empty();
        }

        Path imagePath = cachedImagePath(artist);
        Path missingPath = missingMarkerPath(artist);

        try {
            if (Files.exists(imagePath)) {
                return Optional.of(Files.readAllBytes(imagePath));
            }
            if (Files.exists(missingPath)) {
                return Optional.empty();
            }
        } catch (IOException e) {
            LOG.error("Failed to read cached artist photo for artist {}", artist.getId(), e);
            return Optional.empty();
        }

        return lookupAndCache(artist, imagePath, missingPath);
    }

    private Optional<byte[]> lookupAndCache(Artist artist, Path imagePath, Path missingPath) {
        String thumbUrl = lookupThumbUrl(artist);
        if (thumbUrl == null || thumbUrl.isBlank()) {
            markMissing(missingPath, artist);
            return Optional.empty();
        }

        try {
            byte[] bytes = downloadImage(thumbUrl);
            if (bytes == null) {
                markMissing(missingPath, artist);
                return Optional.empty();
            }
            Files.createDirectories(imagePath.getParent());
            Files.write(imagePath, bytes);
            return Optional.of(bytes);
        } catch (IOException | InterruptedException e) {
            LOG.error("Failed to download artist photo for artist {}", artist.getId(), e);
            return Optional.empty();
        }
    }

    private String lookupThumbUrl(Artist artist) {
        TheAudioDbLookupResponse response;
        try {
            response = theAudioDbClient.lookupByMbid(artist.getMusicbrainzId());
        } catch (Exception e) {
            LOG.error("TheAudioDB lookup failed for artist {}", artist.getId(), e);
            return null;
        }

        if (response == null) {
            return null;
        }

        List<TheAudioDbArtist> artists = response.artists();
        if (artists == null || artists.isEmpty()) {
            return null;
        }

        return artists.get(0).strArtistThumb();
    }

    private byte[] downloadImage(String url) throws IOException, InterruptedException {
        HttpRequest request = HttpRequest.newBuilder(URI.create(url)).GET().build();
        HttpResponse<byte[]> response = httpClient.send(request, HttpResponse.BodyHandlers.ofByteArray());
        if (response.statusCode() / 100 != 2) {
            return null;
        }
        return response.body();
    }

    private void markMissing(Path missingPath, Artist artist) {
        try {
            Files.createDirectories(missingPath.getParent());
            Files.write(missingPath, new byte[0]);
        } catch (IOException e) {
            LOG.error("Failed to write missing-photo marker for artist {}", artist.getId(), e);
        }
    }

    private Path cachedImagePath(Artist artist) {
        return Path.of(storagePath, artist.getId() + ".jpg");
    }

    private Path missingMarkerPath(Artist artist) {
        return Path.of(storagePath, artist.getId() + ".missing");
    }
}

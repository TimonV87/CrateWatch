package com.cratewatch.coverart;

import com.cratewatch.release.domain.CdRelease;
import io.micronaut.context.annotation.Value;
import io.micronaut.http.client.exceptions.HttpClientResponseException;
import jakarta.inject.Singleton;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Optional;

@Singleton
public class CoverArtCacheService {

    private static final Logger LOG = LoggerFactory.getLogger(CoverArtCacheService.class);

    private final CoverArtArchiveClient coverArtArchiveClient;
    private final String storagePath;

    public CoverArtCacheService(CoverArtArchiveClient coverArtArchiveClient,
                                 @Value("${cratewatch.cover-art.storage-path}") String storagePath) {
        this.coverArtArchiveClient = coverArtArchiveClient;
        this.storagePath = storagePath;
    }

    public Optional<byte[]> getCoverArt(CdRelease release) {
        Path imagePath = cachedImagePath(release);
        Path missingPath = missingMarkerPath(release);

        try {
            if (Files.exists(imagePath)) {
                return Optional.of(Files.readAllBytes(imagePath));
            }
            if (Files.exists(missingPath)) {
                return Optional.empty();
            }
        } catch (IOException e) {
            LOG.error("Failed to read cached cover art for release {}", release.getId(), e);
            return Optional.empty();
        }

        if (release.getMusicbrainzReleaseGroupId() == null) {
            return Optional.empty();
        }

        return downloadAndCache(release, imagePath, missingPath);
    }

    private Optional<byte[]> downloadAndCache(CdRelease release, Path imagePath, Path missingPath) {
        try {
            byte[] bytes = coverArtArchiveClient.fetchFrontCover(release.getMusicbrainzReleaseGroupId());
            Files.createDirectories(imagePath.getParent());
            Files.write(imagePath, bytes);
            return Optional.of(bytes);
        } catch (HttpClientResponseException e) {
            if (e.getStatus().getCode() == 404) {
                markMissing(missingPath, release);
                return Optional.empty();
            }
            throw e;
        } catch (IOException e) {
            LOG.error("Failed to cache cover art for release {}", release.getId(), e);
            return Optional.empty();
        }
    }

    private void markMissing(Path missingPath, CdRelease release) {
        try {
            Files.createDirectories(missingPath.getParent());
            Files.write(missingPath, new byte[0]);
        } catch (IOException e) {
            LOG.error("Failed to write missing-cover marker for release {}", release.getId(), e);
        }
    }

    private Path cachedImagePath(CdRelease release) {
        return Path.of(storagePath, release.getId() + ".jpg");
    }

    private Path missingMarkerPath(CdRelease release) {
        return Path.of(storagePath, release.getId() + ".missing");
    }
}

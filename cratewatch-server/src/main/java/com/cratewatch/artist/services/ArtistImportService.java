package com.cratewatch.artist.services;

import com.cratewatch.artist.domain.Artist;
import com.cratewatch.artist.repositories.ArtistRepository;
import com.cratewatch.musicbrainz.MusicBrainzClient;
import com.cratewatch.musicbrainz.domain.MusicBrainzReleaseGroup;
import com.cratewatch.release.domain.CdRelease;
import com.cratewatch.release.repositories.ReleaseRepository;
import com.cratewatch.release.services.ReleaseTypeClassifier;
import lombok.AllArgsConstructor;
import jakarta.inject.Singleton;

import java.time.DateTimeException;
import java.time.LocalDate;
import java.util.List;

@AllArgsConstructor
@Singleton
public class ArtistImportService {

    private static final int PAGE_SIZE = 100;
    private static final int MAX_PAGES = 5;

    private static final List<String> COVER_TYPE_PRIORITY = List.of(
        "STUDIO_ALBUM", "EP", "SINGLE", "COMPILATION", "LIVE", "SOUNDTRACK", "OTHER"
    );

    private final MusicBrainzClient musicBrainzClient;
    private final ArtistRepository artistRepository;
    private final ReleaseRepository releaseRepository;
    private final ReleaseTypeClassifier releaseTypeClassifier;

    public Artist importReleases(Artist artist) {
        int offset = 0;
        for (int page = 0; page < MAX_PAGES; page++) {
            if (page > 0) {
                sleepForRateLimit();
            }

            List<MusicBrainzReleaseGroup> releaseGroups = musicBrainzClient
                .browseReleaseGroups(
                  artist.getMusicbrainzId(), 
                  PAGE_SIZE, 
                  offset
                )
                .releaseGroups();

            for (MusicBrainzReleaseGroup releaseGroup : releaseGroups) {
                importReleaseGroup(artist, releaseGroup);
            }

            if (releaseGroups.size() < PAGE_SIZE) {
                break;
            }
            offset += PAGE_SIZE;
        }

        return assignCoverRelease(artist);
    }

    private void importReleaseGroup(Artist artist, MusicBrainzReleaseGroup releaseGroup) {
        if (releaseRepository.existsByArtistIdAndTitle(artist.getId(), releaseGroup.title())) {
            return;
        }

        String releaseType = releaseTypeClassifier.classify(releaseGroup.primaryType(), releaseGroup.secondaryTypes());
        String coverArtUrl = "https://coverartarchive.org/release-group/" + releaseGroup.id() + "/front-250";
        LocalDate firstReleaseDate = parseReleaseDate(releaseGroup.firstReleaseDate());

        releaseRepository.save(
          new CdRelease(
            artist.getId(),
            releaseGroup.title(),
            releaseType,
            firstReleaseDate,
            releaseGroup.id(),
            coverArtUrl
            )
        );
    }

    /**
     * MusicBrainz dates have variable precision: "", "YYYY", "YYYY-MM", or "YYYY-MM-DD".
     * Missing month/day are normalized to 1 rather than tracked separately.
     */
    private LocalDate parseReleaseDate(String raw) {
        if (raw == null || raw.isBlank()) {
            return null;
        }

        String[] parts = raw.split("-");
        try {
            int year = Integer.parseInt(parts[0]);
            int month = parts.length > 1 ? Integer.parseInt(parts[1]) : 1;
            int day = parts.length > 2 ? Integer.parseInt(parts[2]) : 1;
            return LocalDate.of(year, month, day);
        } catch (NumberFormatException | DateTimeException e) {
            return null;
        }
    }

    private Artist assignCoverRelease(Artist artist) {
        List<CdRelease> releases = releaseRepository.findByArtistId(artist.getId());

        COVER_TYPE_PRIORITY.stream()
            .flatMap(type -> releases.stream().filter(r -> type.equals(r.getReleaseType())))
            .filter(r -> r.getMusicbrainzReleaseGroupId() != null)
            .findFirst()
            .ifPresent(r -> artist.setCoverReleaseId(r.getId()));

        return artistRepository.update(artist);
    }

    private void sleepForRateLimit() {
        try {
            Thread.sleep(1100);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
    }
}

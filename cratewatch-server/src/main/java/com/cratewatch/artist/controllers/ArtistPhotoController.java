package com.cratewatch.artist.controllers;

import com.cratewatch.artist.domain.Artist;
import com.cratewatch.artist.repositories.ArtistRepository;
import com.cratewatch.coverart.CoverArtCacheService;
import com.cratewatch.release.repositories.ReleaseRepository;
import com.cratewatch.theaudiodb.ArtistPhotoCacheService;
import io.micronaut.http.HttpResponse;
import io.micronaut.http.MediaType;
import io.micronaut.http.annotation.Controller;
import io.micronaut.http.annotation.Get;
import io.micronaut.http.annotation.PathVariable;
import io.micronaut.scheduling.TaskExecutors;
import io.micronaut.scheduling.annotation.ExecuteOn;
import io.micronaut.security.annotation.Secured;
import io.micronaut.security.rules.SecurityRule;
import lombok.AllArgsConstructor;

import java.util.Optional;
import java.util.UUID;

@AllArgsConstructor
@Secured(SecurityRule.IS_ANONYMOUS)
@ExecuteOn(TaskExecutors.BLOCKING)
@Controller("/artists")
public class ArtistPhotoController {

    private final ArtistRepository artistRepository;
    private final ReleaseRepository releaseRepository;
    private final ArtistPhotoCacheService artistPhotoCacheService;
    private final CoverArtCacheService coverArtCacheService;

    @Get("/{id}/photo")
    public HttpResponse<byte[]> photo(@PathVariable UUID id) {
        Optional<Artist> artist = artistRepository.findById(id);
        if (artist.isEmpty()) {
            return HttpResponse.notFound();
        }

        return artistPhotoCacheService.getPhoto(artist.get())
            .or(() -> coverFallback(artist.get()))
            .map(bytes -> HttpResponse.ok(bytes).contentType(MediaType.IMAGE_JPEG))
            .orElseGet(HttpResponse::notFound);
    }

    private Optional<byte[]> coverFallback(Artist artist) {
        if (artist.getCoverReleaseId() == null) {
            return Optional.empty();
        }
        return releaseRepository.findById(artist.getCoverReleaseId())
            .flatMap(coverArtCacheService::getCoverArt);
    }
}

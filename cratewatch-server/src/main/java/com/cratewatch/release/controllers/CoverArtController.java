package com.cratewatch.release.controllers;

import com.cratewatch.coverart.CoverArtCacheService;
import com.cratewatch.release.repositories.ReleaseRepository;
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

import java.util.UUID;

@AllArgsConstructor
@Secured(SecurityRule.IS_ANONYMOUS)
@ExecuteOn(TaskExecutors.BLOCKING)
@Controller("/releases")
public class CoverArtController {

    private final ReleaseRepository releaseRepository;
    private final CoverArtCacheService coverArtCacheService;

    @Get("/{id}/cover-art")
    public HttpResponse<byte[]> coverArt(@PathVariable UUID id) {
        return releaseRepository.findById(id)
            .flatMap(release -> coverArtCacheService.getCoverArt(release).map(bytes -> toResponse(bytes)))
            .orElseGet(HttpResponse::notFound);
    }

    private HttpResponse<byte[]> toResponse(byte[] bytes) {
        return HttpResponse.ok(bytes).contentType(MediaType.IMAGE_JPEG);
    }
}

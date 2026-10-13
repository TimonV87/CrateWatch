package com.cratewatch.artist.controllers;

import com.cratewatch.artist.domain.Artist;
import com.cratewatch.artist.domain.ArtistResponse;
import com.cratewatch.artist.domain.ArtistSearchResult;
import com.cratewatch.artist.domain.CreateArtistRequest;
import com.cratewatch.artist.domain.UserArtist;
import com.cratewatch.artist.repositories.ArtistRepository;
import com.cratewatch.artist.repositories.UserArtistRepository;
import com.cratewatch.artist.services.ArtistAlreadyExistsException;
import com.cratewatch.artist.services.ArtistService;
import com.cratewatch.release.domain.CdRelease;
import com.cratewatch.release.domain.ReleaseResponse;
import com.cratewatch.release.domain.UserReleaseState;
import com.cratewatch.release.repositories.ReleaseRepository;
import com.cratewatch.release.repositories.UserReleaseStateRepository;
import com.cratewatch.user.domain.User;
import com.cratewatch.user.repositories.UserRepository;
import io.micronaut.http.HttpResponse;
import io.micronaut.http.HttpStatus;
import io.micronaut.http.annotation.Body;
import io.micronaut.http.annotation.Controller;
import io.micronaut.http.annotation.Error;
import io.micronaut.http.annotation.Get;
import io.micronaut.http.annotation.PathVariable;
import io.micronaut.http.annotation.Post;
import io.micronaut.http.annotation.QueryValue;
import io.micronaut.scheduling.TaskExecutors;
import io.micronaut.scheduling.annotation.ExecuteOn;
import io.micronaut.security.annotation.Secured;
import io.micronaut.security.authentication.Authentication;
import io.micronaut.security.rules.SecurityRule;
import jakarta.validation.Valid;
import lombok.AllArgsConstructor;

import java.util.List;
import java.util.Map;
import java.util.NoSuchElementException;
import java.util.UUID;
import java.util.stream.Collectors;

@AllArgsConstructor
@Secured(SecurityRule.IS_AUTHENTICATED)
@ExecuteOn(TaskExecutors.BLOCKING)
@Controller("/artists")
public class ArtistController {

    private final ArtistRepository artistRepository;
    private final UserArtistRepository userArtistRepository;
    private final ArtistService artistService;
    private final ReleaseRepository releaseRepository;
    private final UserReleaseStateRepository userReleaseStateRepository;
    private final UserRepository userRepository;

    @Get("/search")
    public HttpResponse<List<ArtistSearchResult>> search(@QueryValue String query) {
        return HttpResponse.ok(artistService.searchAlbums(query));
    }

    @Get
    public List<ArtistResponse> list(Authentication authentication) {
        User user = currentUser(authentication);
        List<UUID> artistIds = userArtistRepository.findByUserId(user.getId()).stream()
            .map(UserArtist::getArtistId)
            .toList();
        return artistRepository.findByIdIn(artistIds).stream()
            .map(ArtistResponse::from)
            .toList();
    }

    @Post
    public HttpResponse<ArtistResponse> create(
      @Body @Valid CreateArtistRequest request,
      Authentication authentication
    ) {
        User user = currentUser(authentication);
        Artist artist = artistService.createArtist(request, user);
        return HttpResponse.created(ArtistResponse.from(artist));
    }

    @Error(exception = ArtistAlreadyExistsException.class)
    public HttpResponse<ArtistResponse> onArtistAlreadyExists(ArtistAlreadyExistsException e) {
        return HttpResponse.status(HttpStatus.CONFLICT)
            .body(ArtistResponse.from(e.getExistingArtist()));
    }

    @Get("/{id}")
    public ArtistResponse get(@PathVariable UUID id, Authentication authentication) {
        return ArtistResponse.from(findOwnedArtist(id, authentication));
    }

    @Get("/{id}/releases")
    public List<ReleaseResponse> releases(@PathVariable UUID id, Authentication authentication) {
        User user = currentUser(authentication);
        Artist artist = findOwnedArtist(id, authentication);

        List<CdRelease> releases = releaseRepository.findByArtistIdOrderByFirstReleaseDate(artist.getId());
        List<UUID> releaseIds = releases.stream().map(CdRelease::getId).toList();

        Map<UUID, String> states = userReleaseStateRepository
          .findByUserIdAndCdReleaseIdIn(user.getId(), releaseIds)
          .stream()
          .collect(Collectors.toMap(UserReleaseState::getCdReleaseId, UserReleaseState::getState));
        
        return releases.stream()
            .map(r -> ReleaseResponse.from(r, states.getOrDefault(r.getId(), "NONE")))
            .toList();
    }

    private Artist findOwnedArtist(UUID id, Authentication authentication) {
        User user = currentUser(authentication);
        Artist artist = artistRepository.findById(id)
            .orElseThrow(() -> new NoSuchElementException("No such artist: " + id));
        if (!userArtistRepository.existsByUserIdAndArtistId(user.getId(), id)) {
            throw new NoSuchElementException("No such artist: " + id);
        }
        return artist;
    }

    private User currentUser(Authentication authentication) {
        return userRepository.findByUsername(authentication.getName())
            .orElseThrow(() -> new NoSuchElementException("No such user: " + authentication.getName()));
    }
}

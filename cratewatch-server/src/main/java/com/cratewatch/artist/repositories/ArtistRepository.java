package com.cratewatch.artist.repositories;

import com.cratewatch.artist.domain.Artist;
import io.micronaut.data.jdbc.annotation.JdbcRepository;
import io.micronaut.data.model.query.builder.sql.Dialect;
import io.micronaut.data.repository.CrudRepository;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

@JdbcRepository(dialect = Dialect.POSTGRES)
public interface ArtistRepository extends CrudRepository<Artist, UUID> {

    Optional<Artist> findByMusicbrainzId(String musicbrainzId);

    List<Artist> findByIdIn(List<UUID> ids);
}

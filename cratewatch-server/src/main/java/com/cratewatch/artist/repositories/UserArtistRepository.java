package com.cratewatch.artist.repositories;

import com.cratewatch.artist.domain.UserArtist;
import io.micronaut.data.jdbc.annotation.JdbcRepository;
import io.micronaut.data.model.query.builder.sql.Dialect;
import io.micronaut.data.repository.CrudRepository;

import java.util.List;
import java.util.UUID;

@JdbcRepository(dialect = Dialect.POSTGRES)
public interface UserArtistRepository extends CrudRepository<UserArtist, UUID> {

    List<UserArtist> findByUserId(UUID userId);

    boolean existsByUserIdAndArtistId(UUID userId, UUID artistId);
}

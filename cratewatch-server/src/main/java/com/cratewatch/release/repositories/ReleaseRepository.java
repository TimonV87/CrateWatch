package com.cratewatch.release.repositories;

import com.cratewatch.release.domain.CdRelease;
import io.micronaut.data.jdbc.annotation.JdbcRepository;
import io.micronaut.data.model.query.builder.sql.Dialect;
import io.micronaut.data.repository.CrudRepository;

import java.util.List;
import java.util.UUID;

@JdbcRepository(dialect = Dialect.POSTGRES)
public interface ReleaseRepository extends CrudRepository<CdRelease, UUID> {

    List<CdRelease> findByArtistId(UUID artistId);

    boolean existsByArtistIdAndTitle(UUID artistId, String title);
}

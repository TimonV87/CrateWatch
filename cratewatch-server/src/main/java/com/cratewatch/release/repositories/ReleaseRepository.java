package com.cratewatch.release.repositories;

import com.cratewatch.release.domain.CdRelease;
import io.micronaut.data.annotation.Query;
import io.micronaut.data.jdbc.annotation.JdbcRepository;
import io.micronaut.data.model.query.builder.sql.Dialect;
import io.micronaut.data.repository.CrudRepository;

import java.util.List;
import java.util.UUID;

@JdbcRepository(dialect = Dialect.POSTGRES)
public interface ReleaseRepository extends CrudRepository<CdRelease, UUID> {

    List<CdRelease> findByArtistId(UUID artistId);

    @Query("SELECT * FROM cd_releases WHERE artist_id = :artistId ORDER BY first_release_date NULLS LAST")
    List<CdRelease> findByArtistIdOrderByFirstReleaseDate(UUID artistId);

    boolean existsByArtistIdAndTitle(UUID artistId, String title);
}

package com.cdwatcher.user.controllers;

import com.cdwatcher.user.domain.AppUser;
import com.cdwatcher.user.domain.RegisterRequest;
import com.cdwatcher.user.domain.RegisterResponse;
import com.cdwatcher.user.repositories.UserRepository;
import io.micronaut.http.HttpResponse;
import io.micronaut.http.HttpStatus;
import io.micronaut.http.annotation.Body;
import io.micronaut.http.annotation.Controller;
import io.micronaut.http.annotation.Post;
import jakarta.validation.Valid;
import lombok.AllArgsConstructor;
import org.mindrot.jbcrypt.BCrypt;

@AllArgsConstructor
@Controller("/auth")
public class RegisterController {

    private final UserRepository userRepository;

    @Post("/register")
    public HttpResponse<RegisterResponse> register(@Body @Valid RegisterRequest request) {
        if (userRepository.existsByUsername(request.username())) {
            return HttpResponse.status(HttpStatus.CONFLICT);
        }
        String passwordHash = BCrypt.hashpw(request.password(), BCrypt.gensalt());
        AppUser saved = userRepository.save(new AppUser(request.username(), passwordHash));
        return HttpResponse.created(new RegisterResponse(saved.getId(), saved.getUsername()));
    }
}

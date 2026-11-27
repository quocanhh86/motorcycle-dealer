(function () {
  'use strict';

  angular
    .module('app.core')
    .run(registerRouteGuards);

  registerRouteGuards.$inject = ['$transitions', 'AuthService', 'ROLE_HOME'];
  function registerRouteGuards($transitions, AuthService, ROLE_HOME) {
    // 1. Redirect already-authenticated principals away from /login and /register
    $transitions.onBefore(
      {
        to: function (state) {
          return state.name === 'app.public.login' || state.name === 'app.public.register';
        }
      },
      function redirectAuthenticatedEntry(transition) {
        if (AuthService.isAuthenticated()) {
          var targetHome = ROLE_HOME[AuthService.getRole()] || 'app.public.showroom';
          return transition.router.stateService.target(targetHome);
        }
        return true;
      }
    );

    // 2. Enforce requiresAuth, canonical role membership, and granular 34-permission checks
    $transitions.onBefore(
      {
        to: function (state) {
          return Boolean(
            state.data && (state.data.requiresAuth || state.data.roles || state.data.permissions)
          );
        }
      },
      function enforceRbacGuard(transition) {
        var $state = transition.router.stateService;
        var mergedPolicy = mergeEnteringStatePolicies(transition);

        return AuthService.ensureValidSession().then(function (authenticated) {
          if (!authenticated) {
            var requestedState = transition.to().name;
            return $state.target('app.public.login', {
              returnUrl: requestedState,
              returnTo: requestedState
            });
          }

          if (mergedPolicy.roles && mergedPolicy.roles.length > 0) {
            var callerRole = AuthService.getRole();
            if (mergedPolicy.roles.indexOf(callerRole) === -1) {
              return $state.target('app.public.forbidden');
            }
          }

          if (mergedPolicy.permissions && mergedPolicy.permissions.length > 0) {
            var allowed = AuthService.hasPermissions(
              mergedPolicy.permissions,
              mergedPolicy.permissionMode || 'ALL'
            );
            if (!allowed) {
              return $state.target('app.public.forbidden');
            }
          }

          return true;
        });
      }
    );

    function mergeEnteringStatePolicies(transition) {
      var pathNodes = transition.treeChanges && transition.treeChanges('to');
      var statesToInspect = [];

      if (pathNodes && pathNodes.length > 0) {
        for (var i = 0; i < pathNodes.length; i++) {
          if (pathNodes[i].state && pathNodes[i].state.self) {
            statesToInspect.push(pathNodes[i].state.self);
          }
        }
      } else {
        statesToInspect.push(transition.to());
      }

      return statesToInspect.reduce(function (acc, st) {
        var d = (st && st.data) || {};
        return {
          requiresAuth: Boolean(acc.requiresAuth || d.requiresAuth),
          roles: d.roles || acc.roles,
          permissions: acc.permissions.concat(d.permissions || []),
          permissionMode: d.permissionMode || acc.permissionMode || 'ALL'
        };
      }, { requiresAuth: false, roles: null, permissions: [], permissionMode: 'ALL' });
    }
  }
})();

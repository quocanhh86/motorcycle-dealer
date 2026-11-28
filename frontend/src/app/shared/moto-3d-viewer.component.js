(function () {
  'use strict';

  var VI_PATCH = {
    moto3d: {
      badgeLive: 'HD 360° XE THẬT • NHẤN GHIM ĐỂ SOI LINH KIỆN',
      dragHint: 'Kéo ngang đổi góc HD • Cuộn phóng to tới 2.6x • Nhấn ghim đỏ soi chi tiết thật',
      autoSpin: 'Tự quay 360°',
      hotspotsToggle: 'Ghim linh kiện',
      resetView: 'Toàn cảnh 1x',
      zoomInAria: 'Phóng to ảnh xe thật HD',
      zoomOutAria: 'Thu nhỏ ảnh xe thật HD',
      prevAngleAria: 'Góc chụp xe thật trước',
      nextAngleAria: 'Góc chụp xe thật tiếp theo',
      cameraPresetsLabel: '4 Góc Chụp Xe Thật HD Showroom',
      lightingLabel: 'Ánh sáng & Sắc độ Showroom thực tế',
      partsLabel: 'Soi Cận Cảnh 8 Cụm Linh Kiện OEM Trên Xe Thật',
      camFront: '3/4 Trước',
      camSide: 'Ngang Thân Xe',
      camRear: 'Đuôi & Đèn LED',
      camCockpit: 'Buồng Lái & Đồng Hồ',
      toneDaylight: 'Showroom Tự Nhiên',
      toneCrisp: 'Siêu Nét HD',
      toneGolden: 'Nắng Chiều Ấm',
      toneSpotlight: 'Tiêu Điểm Đêm',
      inspectorEyebrow: 'SOI CẬN CẢNH XE THẬT HD • OEM SPEC',
      inspectorClear: 'Xem toàn xe',
      partEngineName: 'Động cơ & Lốc máy OEM',
      partEngineDesc: 'Khối động cơ {{cc}} cc tiêu chuẩn nhà máy với lốc nhôm đúc áp lực tản nhiệt tối ưu. Khuyến nghị thay nhớt & lọc chính hãng định kỳ mỗi 4.000 km.',
      partSuspensionName: 'Phuộc trước & Phanh đĩa ABS',
      partSuspensionDesc: 'Cụm giảm xóc trước ổn định thân xe kết hợp đĩa phanh khoan lỗ tản nhiệt cùng heo dầu hiệu suất cao tích hợp chống bó cứng ABS.',
      partHeadlightName: 'Cụm đèn pha Full-LED & DRL',
      partHeadlightDesc: 'Hệ thống chiếu sáng LED cường độ cao đa tầng kết hợp dải định vị ban ngày sắc nét, đảm bảo tầm nhìn tối ưu trong mọi điều kiện thời tiết.',
      partBodyworkName: 'Dàn áo & Lớp sơn Clear-Coat',
      partBodyworkDesc: 'Thiết kế khí động học sắc sảo phủ sơn nhiều lớp ánh kim/nhám chuẩn hãng chống tia UV và bền màu trong điều kiện vận hành thực tế.',
      partExhaustName: 'Hệ thống ống xả & Tiêu âm',
      partExhaustDesc: 'Cổ xả hợp kim chống ăn mòn nối liền lon pô đạt chuẩn khí thải Euro, đi kèm ốp chống nóng bảo vệ an toàn cho người ngồi sau.',
      partCockpitName: 'Buồng lái & Đồng hồ kỹ thuật số',
      partCockpitDesc: 'Mặt đồng hồ điện tử độ tương phản cao hiển thị rõ tốc độ, tua máy, cấp số, hành trình và cảnh báo ABS cùng cụm công tắc công thái học.',
      partWheelsName: 'Mâm đúc hợp kim & Lốp không săm',
      partWheelsDesc: 'Bộ vành đúc đa chấu trọng lượng nhẹ kết hợp lốp không săm rãnh sâu tăng độ bám đường khi vào cua và phanh gấp.',
      partTailSeatName: 'Yên xe công thái học & Đuôi LED',
      partTailSeatDesc: 'Yên da chống trượt nâng đỡ tối ưu cho người lái và hành khách, hoàn thiện cùng cụm đèn hậu LED nhận diện nổi bật từ phía sau.'
    }
  };

  var EN_PATCH = {
    moto3d: {
      badgeLive: 'REAL HD 360° • TAP HOTSPOTS TO INSPECT',
      dragHint: 'Drag horizontally to scrub HD angles • Scroll to zoom up to 2.6x • Click pins to inspect',
      autoSpin: 'Auto-Tour',
      hotspotsToggle: 'Hotspots',
      resetView: 'Reset 1x',
      zoomInAria: 'Zoom into real HD motorcycle photo',
      zoomOutAria: 'Zoom out of real HD motorcycle photo',
      prevAngleAria: 'Previous real HD showroom angle',
      nextAngleAria: 'Next real HD showroom angle',
      cameraPresetsLabel: '4 Real-Life HD Showroom Angles',
      lightingLabel: 'Real Showroom Lighting & Tone',
      partsLabel: 'Inspect 8 Real OEM Mechanical Areas',
      camFront: 'Front 3/4',
      camSide: 'Side Profile',
      camRear: 'Rear & LED',
      camCockpit: 'Cockpit & Dash',
      toneDaylight: 'Natural Showroom',
      toneCrisp: 'Ultra Crisp HD',
      toneGolden: 'Warm Daylight',
      toneSpotlight: 'Evening Spotlight',
      inspectorEyebrow: 'REAL HD CLOSE-UP • OEM SPEC',
      inspectorClear: 'Full Bike View',
      partEngineName: 'OEM Engine & Crankcase',
      partEngineDesc: '{{cc}} cc factory-calibrated powerplant with die-cast aluminum crankcase and precision thermal management. Recommended OEM oil service every 4,000 km.',
      partSuspensionName: 'Front Forks & ABS Disc Brake',
      partSuspensionDesc: 'Precision-damped front suspension paired with drilled stainless steel brake rotor and high-bite ABS caliper for confident stopping power.',
      partHeadlightName: 'Full-LED Headlamp & DRL',
      partHeadlightDesc: 'High-output multi-reflector LED optics paired with signature daytime running light blades for crisp visibility day and night.',
      partBodyworkName: 'Aerodynamic Fairings & Clear-Coat',
      partBodyworkDesc: 'Multi-layered sculpted bodywork finished in authentic factory automotive clear-coat for deep showroom gloss and UV durability.',
      partExhaustName: 'Exhaust Header & Muffler',
      partExhaustDesc: 'Corrosion-resistant exhaust routing paired with emissions-compliant OEM silencer and protective heat shield.',
      partCockpitName: 'Cockpit & Digital Instrument Dash',
      partCockpitDesc: 'High-contrast digital gauge cluster displaying speed, RPM, fuel, trip telemetry, and ABS indicators alongside ergonomic switchgear.',
      partWheelsName: 'Cast Alloy Wheels & Tubeless Tires',
      partWheelsDesc: 'Lightweight multi-spoke alloy rims wrapped in deep-tread tubeless tires engineered for wet and dry cornering grip.',
      partTailSeatName: 'Ergonomic Saddle & LED Tail Unit',
      partTailSeatDesc: 'Anti-slip sculpted rider/pillion seating paired with sharp signature LED brake optics and clean tail geometry.'
    }
  };

  var FALLBACK_JPG = '/assets/editorial/showroom-hero-apex.jpg';

  var BUNDLED_SLUGS = {
    'honda-airblade-160': true,
    'honda-sh160i-sport': true,
    'honda-winner-x': true,
    'yamaha-exciter-155': true,
    'honda-cb300r': true,
    'honda-nx500': true
  };

  var MODEL_CODE_TO_SLUG = {
    'HND-AB160-2026': 'honda-airblade-160',
    'HND-SH160I-2026': 'honda-sh160i-sport',
    'HND-WINX-2026': 'honda-winner-x',
    'YMH-EXC155-2026': 'yamaha-exciter-155',
    'HND-CB300R-2026': 'honda-cb300r',
    'HND-NX500-2026': 'honda-nx500'
  };

  var ANGLE_DEFS = [
    { key: 'front', suffix: '', labelKey: 'moto3d.camFront' },
    { key: 'side', suffix: '-side', labelKey: 'moto3d.camSide' },
    { key: 'rear', suffix: '-rear', labelKey: 'moto3d.camRear' },
    { key: 'cockpit', suffix: '-cockpit', labelKey: 'moto3d.camCockpit' }
  ];

  var LIGHTING_TONES = [
    { key: 'daylight', labelKey: 'moto3d.toneDaylight', swatchClass: 'moto3d-tone-daylight', cssClass: 'tone-daylight' },
    { key: 'crisp', labelKey: 'moto3d.toneCrisp', swatchClass: 'moto3d-tone-crisp', cssClass: 'tone-crisp' },
    { key: 'golden', labelKey: 'moto3d.toneGolden', swatchClass: 'moto3d-tone-golden', cssClass: 'tone-golden' },
    { key: 'spotlight', labelKey: 'moto3d.toneSpotlight', swatchClass: 'moto3d-tone-spotlight', cssClass: 'tone-spotlight' }
  ];

  /* Each OEM component maps to its best real-life photo angle + accurate focus point (x%, y%) on every visible angle */
  var COMPONENT_META = [
    {
      id: 'headlight',
      icon: 'sun',
      nameKey: 'moto3d.partHeadlightName',
      descKey: 'moto3d.partHeadlightDesc',
      bestAngle: 'front',
      zoom: 1.85,
      pins: {
        front: { x: 61, y: 41 },
        cockpit: { x: 50, y: 24 }
      }
    },
    {
      id: 'frontSuspension',
      icon: 'wrench',
      nameKey: 'moto3d.partSuspensionName',
      descKey: 'moto3d.partSuspensionDesc',
      bestAngle: 'front',
      zoom: 1.95,
      pins: {
        front: { x: 60, y: 65 },
        side: { x: 69, y: 60 }
      }
    },
    {
      id: 'bodywork',
      icon: 'sparkle',
      nameKey: 'moto3d.partBodyworkName',
      descKey: 'moto3d.partBodyworkDesc',
      bestAngle: 'front',
      zoom: 1.7,
      pins: {
        front: { x: 52, y: 46 },
        side: { x: 55, y: 44 }
      }
    },
    {
      id: 'engine',
      icon: 'gear',
      nameKey: 'moto3d.partEngineName',
      descKey: 'moto3d.partEngineDesc',
      bestAngle: 'side',
      zoom: 1.95,
      pins: {
        front: { x: 45, y: 61 },
        side: { x: 52, y: 63 }
      }
    },
    {
      id: 'exhaust',
      icon: 'flame',
      nameKey: 'moto3d.partExhaustName',
      descKey: 'moto3d.partExhaustDesc',
      bestAngle: 'side',
      zoom: 1.95,
      pins: {
        front: { x: 35, y: 63 },
        side: { x: 34, y: 66 },
        rear: { x: 63, y: 64 }
      }
    },
    {
      id: 'wheels',
      icon: 'circle',
      nameKey: 'moto3d.partWheelsName',
      descKey: 'moto3d.partWheelsDesc',
      bestAngle: 'side',
      zoom: 1.85,
      pins: {
        front: { x: 62, y: 77 },
        side: { x: 72, y: 74 },
        rear: { x: 48, y: 74 }
      }
    },
    {
      id: 'tailSeat',
      icon: 'motorcycle',
      nameKey: 'moto3d.partTailSeatName',
      descKey: 'moto3d.partTailSeatDesc',
      bestAngle: 'rear',
      zoom: 1.8,
      pins: {
        side: { x: 37, y: 43 },
        rear: { x: 48, y: 45 }
      }
    },
    {
      id: 'cockpit',
      icon: 'gauge',
      nameKey: 'moto3d.partCockpitName',
      descKey: 'moto3d.partCockpitDesc',
      bestAngle: 'cockpit',
      zoom: 1.65,
      pins: {
        cockpit: { x: 50, y: 50 }
      }
    }
  ];

  registerMoto3dI18n.$inject = ['I18nService'];
  function registerMoto3dI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.shared')
    .run(registerMoto3dI18n)
    .component('moto3dViewer', {
      bindings: {
        model: '<',
        modelName: '@',
        modelCode: '@',
        category: '@',
        displacementCc: '<'
      },
      controllerAs: '$ctrl',
      controller: Moto3dViewerController,
      template:
        '<div class="moto3d-shell">' +
          '<div class="moto3d-stage" ng-class="$ctrl.activeToneClass">' +
            '<div class="moto3d-photo-viewport" ' +
                 'tabindex="0" ' +
                 'role="region" ' +
                 'ng-attr-aria-label="{{ $ctrl.resolvedModelName }} — {{ \'moto3d.badgeLive\' | t }}" ' +
                 'ng-class="{\'is-zoomed\': $ctrl.zoom > 1.05, \'is-dragging\': $ctrl.isDragging}">' +
              '<div class="moto3d-photo-track" ng-style="$ctrl.stageTransformStyle">' +
                '<div class="moto3d-photo-slide" ' +
                     'ng-repeat="slide in $ctrl.slides track by slide.key" ' +
                     'ng-class="{\'is-active\': $ctrl.activeAngleIndex === $index}">' +
                  '<picture>' +
                    '<source ng-if="slide.webpUrl" ng-attr-srcset="{{ slide.webpUrl }}" type="image/webp" />' +
                    '<img class="moto3d-photo-img" ' +
                         'ng-attr-src="{{ slide.jpgUrl }}" ' +
                         'ng-attr-alt="{{ $ctrl.resolvedModelName }} — {{ slide.labelKey | t }}" ' +
                         'draggable="false" ' +
                         'decoding="async" />' +
                  '</picture>' +
                '</div>' +

                '<div class="moto3d-hotspot-layer" ng-if="$ctrl.showHotspots">' +
                  '<button type="button" class="moto3d-hotspot-pin" ' +
                          'ng-repeat="pin in $ctrl.visibleHotspots track by pin.id" ' +
                          'ng-class="{\'is-active\': $ctrl.activePart && $ctrl.activePart.id === pin.id}" ' +
                          'ng-style="{ left: pin.x + \'%\', top: pin.y + \'%\', transform: $ctrl.pinCounterTransform }" ' +
                          'ng-attr-aria-label="{{ pin.nameKey | t }}" ' +
                          'ng-attr-aria-pressed="{{ ($ctrl.activePart && $ctrl.activePart.id === pin.id) ? \'true\' : \'false\' }}" ' +
                          'ng-click="$ctrl.onHotspotClick($event, pin.id)">' +
                    '<span class="moto3d-hotspot-pulse" aria-hidden="true"></span>' +
                    '<span class="moto3d-hotspot-core" aria-hidden="true"></span>' +
                    '<span class="moto3d-hotspot-tip">{{ pin.nameKey | t }}</span>' +
                  '</button>' +
                '</div>' +
              '</div>' +
            '</div>' +

            '<div class="moto3d-vignette-overlay" aria-hidden="true"></div>' +

            '<button type="button" class="moto3d-stage-nav is-prev" ' +
                    'ng-if="$ctrl.slides.length > 1" ' +
                    'ng-attr-aria-label="{{ \'moto3d.prevAngleAria\' | t }}" ' +
                    'ng-click="$ctrl.stepAngle(-1)">' +
              '<app-icon name="caret-left" weight="regular" size="18"></app-icon>' +
            '</button>' +
            '<button type="button" class="moto3d-stage-nav is-next" ' +
                    'ng-if="$ctrl.slides.length > 1" ' +
                    'ng-attr-aria-label="{{ \'moto3d.nextAngleAria\' | t }}" ' +
                    'ng-click="$ctrl.stepAngle(1)">' +
              '<app-icon name="caret-right" weight="regular" size="18"></app-icon>' +
            '</button>' +

            '<div class="moto3d-hud-top">' +
              '<span class="moto3d-hud-badge mono-num">' +
                '<app-icon name="camera" weight="duotone" size="14"></app-icon>' +
                '<span>{{ \'moto3d.badgeLive\' | t }}</span>' +
              '</span>' +
              '<div class="moto3d-hud-actions">' +
                '<button type="button" class="moto3d-hud-btn" ng-class="{\'is-active\': $ctrl.autoTour}" ng-attr-aria-pressed="{{ $ctrl.autoTour ? \'true\' : \'false\' }}" ng-click="$ctrl.toggleAutoTour()">' +
                  '<app-icon name="arrows-clockwise" weight="regular" size="14"></app-icon>' +
                  '<span>{{ \'moto3d.autoSpin\' | t }}</span>' +
                '</button>' +
                '<button type="button" class="moto3d-hud-btn" ng-class="{\'is-active\': $ctrl.showHotspots}" ng-attr-aria-pressed="{{ $ctrl.showHotspots ? \'true\' : \'false\' }}" ng-click="$ctrl.toggleHotspots()">' +
                  '<app-icon name="crosshair" weight="duotone" size="14"></app-icon>' +
                  '<span>{{ \'moto3d.hotspotsToggle\' | t }}</span>' +
                '</button>' +
                '<button type="button" class="moto3d-hud-btn" ng-attr-aria-label="{{ \'moto3d.zoomInAria\' | t }}" ng-click="$ctrl.zoomBy(0.35)">' +
                  '<app-icon name="plus" weight="regular" size="13"></app-icon>' +
                '</button>' +
                '<button type="button" class="moto3d-hud-btn" ng-attr-aria-label="{{ \'moto3d.zoomOutAria\' | t }}" ng-click="$ctrl.zoomBy(-0.35)">' +
                  '<app-icon name="minus" weight="regular" size="13"></app-icon>' +
                '</button>' +
                '<button type="button" class="moto3d-hud-btn" ng-click="$ctrl.resetView()">' +
                  '<app-icon name="arrow-counter-clockwise" weight="regular" size="13"></app-icon>' +
                  '<span>{{ \'moto3d.resetView\' | t }}</span>' +
                '</button>' +
              '</div>' +
            '</div>' +

            '<div class="moto3d-hud-bottom">' +
              '<div class="moto3d-inspector" ng-if="$ctrl.activePart">' +
                '<div class="moto3d-inspector-head">' +
                  '<span class="moto3d-inspector-eyebrow">{{ \'moto3d.inspectorEyebrow\' | t }}</span>' +
                  '<button type="button" class="moto3d-hud-btn moto3d-inspector-close" ng-click="$ctrl.clearSelectedPart()">' +
                    '<span>{{ \'moto3d.inspectorClear\' | t }}</span>' +
                  '</button>' +
                '</div>' +
                '<div class="moto3d-inspector-title">{{ $ctrl.activePart.nameKey | t }}</div>' +
                '<div class="moto3d-inspector-desc">{{ $ctrl.activePart.descKey | t:{ cc: $ctrl.resolvedCc } }}</div>' +
              '</div>' +
              '<div class="moto3d-status-bar">' +
                '<span class="moto3d-hint-pill mono-num">{{ $ctrl.activeAngleIndex + 1 }}/{{ $ctrl.slides.length }} • {{ $ctrl.activeSlideLabelKey | t }} • {{ $ctrl.zoomDisplay }}</span>' +
                '<span class="moto3d-hint-pill moto3d-hint-secondary">{{ \'moto3d.dragHint\' | t }}</span>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="moto3d-controls-grid">' +
            '<div class="moto3d-panel">' +
              '<div class="moto3d-panel-label">' +
                '<span>{{ \'moto3d.cameraPresetsLabel\' | t }}</span>' +
                '<span class="mono-num">{{ $ctrl.resolvedModelCode }}</span>' +
              '</div>' +
              '<div class="moto3d-angle-thumbs" role="group" ng-attr-aria-label="{{ \'moto3d.cameraPresetsLabel\' | t }}">' +
                '<button type="button" class="moto3d-angle-thumb" ' +
                        'ng-repeat="slide in $ctrl.slides track by slide.key" ' +
                        'ng-class="{\'is-active\': $ctrl.activeAngleIndex === $index}" ' +
                        'ng-attr-aria-pressed="{{ $ctrl.activeAngleIndex === $index ? \'true\' : \'false\' }}" ' +
                        'ng-click="$ctrl.selectAngleIndex($index)">' +
                  '<picture>' +
                    '<source ng-if="slide.webpUrl" ng-attr-srcset="{{ slide.webpUrl }}" type="image/webp" />' +
                    '<img class="moto3d-angle-thumb-img" ng-attr-src="{{ slide.jpgUrl }}" alt="" loading="lazy" decoding="async" />' +
                  '</picture>' +
                  '<span class="moto3d-angle-thumb-label">{{ slide.labelKey | t }}</span>' +
                '</button>' +
              '</div>' +

              '<div class="moto3d-panel-label" style="margin-top:4px;">{{ \'moto3d.lightingLabel\' | t }}</div>' +
              '<div class="moto3d-chip-row" role="group" ng-attr-aria-label="{{ \'moto3d.lightingLabel\' | t }}">' +
                '<button type="button" class="moto3d-swatch-btn" ' +
                        'ng-repeat="tone in $ctrl.lightingTones track by tone.key" ' +
                        'ng-class="{\'is-active\': $ctrl.activeToneKey === tone.key}" ' +
                        'ng-attr-aria-pressed="{{ $ctrl.activeToneKey === tone.key ? \'true\' : \'false\' }}" ' +
                        'ng-click="$ctrl.selectTone(tone.key)">' +
                  '<span class="moto3d-swatch-dot" ng-class="tone.swatchClass" aria-hidden="true"></span>' +
                  '<span>{{ tone.labelKey | t }}</span>' +
                '</button>' +
              '</div>' +
            '</div>' +

            '<div class="moto3d-panel">' +
              '<div class="moto3d-panel-label">' +
                '<span>{{ \'moto3d.partsLabel\' | t }}</span>' +
                '<span class="mono-num">{{ $ctrl.resolvedCc }} cc</span>' +
              '</div>' +
              '<div class="moto3d-chip-row" role="group" ng-attr-aria-label="{{ \'moto3d.partsLabel\' | t }}">' +
                '<button type="button" class="moto3d-chip" ' +
                        'ng-repeat="part in $ctrl.componentList track by part.id" ' +
                        'ng-class="{\'is-active\': $ctrl.activePart && $ctrl.activePart.id === part.id}" ' +
                        'ng-attr-aria-pressed="{{ ($ctrl.activePart && $ctrl.activePart.id === part.id) ? \'true\' : \'false\' }}" ' +
                        'ng-click="$ctrl.focusComponent(part.id)">' +
                  '<app-icon name="{{ part.icon }}" weight="duotone" size="13"></app-icon>' +
                  '<span>{{ part.nameKey | t }}</span>' +
                '</button>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</div>'
    });

  Moto3dViewerController.$inject = ['$element', '$window', '$rootScope', '$scope', '$interval', 'I18nService'];
  function Moto3dViewerController($element, $window, $rootScope, $scope, $interval, I18nService) {
    var $ctrl = this;
    var isDestroyed = false;
    var isInitialized = false;
    var lastModelIdentity = '';
    var tourTimer = null;
    var cleanupListeners = [];

    var dragState = {
      active: false,
      pointerId: null,
      startX: 0,
      startY: 0,
      lastX: 0,
      lastY: 0,
      scrubAccum: 0,
      moved: false
    };

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }

    function resolveSlug() {
      var code = String(($ctrl.model && $ctrl.model.modelCode) || $ctrl.modelCode || '').trim().toUpperCase();
      if (code && MODEL_CODE_TO_SLUG[code]) {
        return MODEL_CODE_TO_SLUG[code];
      }
      var rawUrl = String(($ctrl.model && $ctrl.model.imageUrl) || '').trim();
      var match = rawUrl.match(/\/assets\/models\/([a-z0-9-]+?)(?:-(?:side|rear|cockpit))?\.(?:jpe?g|webp|png)(?:\?.*)?$/i);
      if (match && BUNDLED_SLUGS[match[1].toLowerCase()]) {
        return match[1].toLowerCase();
      }
      return null;
    }

    function buildSlides() {
      var slug = resolveSlug();
      if (slug && BUNDLED_SLUGS[slug]) {
        return ANGLE_DEFS.map(function (def) {
          var base = '/assets/models/' + slug + def.suffix;
          return {
            key: def.key,
            labelKey: def.labelKey,
            webpUrl: base + '.webp',
            jpgUrl: base + '.jpg'
          };
        });
      }
      var customUrl = String(($ctrl.model && $ctrl.model.imageUrl) || '').trim() || FALLBACK_JPG;
      var derivedWebp = /^\/assets\/.*\.(jpe?g|png)$/i.test(customUrl)
        ? customUrl.replace(/\.(jpe?g|png)$/i, '.webp')
        : null;
      return [{
        key: 'front',
        labelKey: 'moto3d.camFront',
        webpUrl: derivedWebp,
        jpgUrl: customUrl
      }];
    }

    function computeModelIdentity() {
      var m = $ctrl.model || {};
      var idPart = m.id !== undefined && m.id !== null ? String(m.id) : '';
      var codePart = String(m.modelCode || $ctrl.modelCode || '').trim().toUpperCase();
      var imgPart = String(m.imageUrl || '').trim();
      return idPart + '|' + codePart + '|' + imgPart;
    }

    function syncModelMetadata() {
      var m = $ctrl.model || {};
      $ctrl.resolvedModelName = String(m.modelName || m.motorcycleModelName || $ctrl.modelName || 'Apex Motorcycle').trim();
      $ctrl.resolvedModelCode = String(m.modelCode || $ctrl.modelCode || 'APEX-2026').trim();
      $ctrl.resolvedCc = Number(m.engineDisplacementCc || $ctrl.displacementCc || 160) || 160;
      $ctrl.slides = buildSlides();
      if ($ctrl.activeAngleIndex >= $ctrl.slides.length) {
        $ctrl.activeAngleIndex = 0;
      }
      updateActiveAngleMetadata();
    }

    function clampPan() {
      var maxPan = Math.max(0, (($ctrl.zoom - 1) / (2 * $ctrl.zoom)) * 100);
      $ctrl.panX = Math.max(-maxPan, Math.min(maxPan, $ctrl.panX));
      $ctrl.panY = Math.max(-maxPan, Math.min(maxPan, $ctrl.panY));
    }

    function updateStageTransform() {
      clampPan();
      var tiltStr = (!$ctrl.isDragging || $ctrl.zoom > 1.05 || !$ctrl.tiltDeg)
        ? ''
        : ('perspective(1200px) rotateY(' + $ctrl.tiltDeg.toFixed(2) + 'deg) ');
      var scaleStr = 'scale(' + $ctrl.zoom.toFixed(3) + ') ';
      var panStr = 'translate3d(' + $ctrl.panX.toFixed(2) + '%, ' + $ctrl.panY.toFixed(2) + '%, 0)';
      $ctrl.stageTransformStyle = {
        transform: tiltStr + scaleStr + panStr
      };
      $ctrl.pinCounterTransform = 'translate(-50%, -50%) scale(' + (1 / $ctrl.zoom).toFixed(3) + ')';
      $ctrl.zoomDisplay = $ctrl.zoom.toFixed(1) + 'x';
    }

    function updateActiveAngleMetadata() {
      var slide = ($ctrl.slides && $ctrl.slides[$ctrl.activeAngleIndex]) || { key: 'front', labelKey: 'moto3d.camFront' };
      $ctrl.activeAngleKey = slide.key;
      $ctrl.activeSlideLabelKey = slide.labelKey;

      var pins = [];
      for (var i = 0; i < COMPONENT_META.length; i++) {
        var part = COMPONENT_META[i];
        var coord = part.pins && part.pins[slide.key];
        if (coord) {
          pins.push({
            id: part.id,
            nameKey: part.nameKey,
            x: coord.x,
            y: coord.y
          });
        }
      }
      $ctrl.visibleHotspots = pins;
    }

    function stopAutoTourTimer() {
      if (tourTimer) {
        $interval.cancel(tourTimer);
        tourTimer = null;
      }
    }

    function syncAutoTourTimer() {
      stopAutoTourTimer();
      if (isDestroyed || !$ctrl.autoTour || !$ctrl.slides || $ctrl.slides.length <= 1) {
        return;
      }
      tourTimer = $interval(function () {
        if (isDestroyed || dragState.active || $ctrl.activePart) {
          return;
        }
        $ctrl.activeAngleIndex = ($ctrl.activeAngleIndex + 1) % $ctrl.slides.length;
        $ctrl.zoom = 1.0;
        $ctrl.panX = 0;
        $ctrl.panY = 0;
        $ctrl.tiltDeg = 0;
        updateActiveAngleMetadata();
        updateStageTransform();
      }, 3800);
    }

    function isReducedMotionPreferred() {
      try {
        return Boolean(
          $window &&
          angular.isFunction($window.matchMedia) &&
          $window.matchMedia('(prefers-reduced-motion: reduce)').matches
        );
      } catch (e) {
        return false;
      }
    }

    function bindViewportEvents() {
      try {
        var rootEl = $element && $element[0];
        if (!rootEl || !angular.isFunction(rootEl.querySelector)) {
          return;
        }
        var viewport = rootEl.querySelector('.moto3d-photo-viewport');
        if (!viewport || !angular.isFunction(viewport.addEventListener)) {
          return;
        }

        function onPointerDown(evt) {
          if (evt.pointerType === 'mouse' && evt.button !== 0) {
            return;
          }
          if (evt.target && angular.isFunction(evt.target.closest) && evt.target.closest('.moto3d-hotspot-pin')) {
            return;
          }
          dragState.active = true;
          dragState.pointerId = evt.pointerId;
          dragState.startX = evt.clientX;
          dragState.startY = evt.clientY;
          dragState.lastX = evt.clientX;
          dragState.lastY = evt.clientY;
          dragState.scrubAccum = 0;
          dragState.moved = false;
          $ctrl.isDragging = true;
          if ($ctrl.autoTour) {
            $ctrl.autoTour = false;
            stopAutoTourTimer();
          }
          if (angular.isFunction(viewport.setPointerCapture) && evt.pointerId !== undefined) {
            try { viewport.setPointerCapture(evt.pointerId); } catch (e) { /* noop */ }
          }
          $scope.$applyAsync(angular.noop);
        }

        function onPointerMove(evt) {
          if (!dragState.active || (dragState.pointerId !== null && evt.pointerId !== dragState.pointerId)) {
            return;
          }
          var dx = evt.clientX - dragState.lastX;
          var dy = evt.clientY - dragState.lastY;
          dragState.lastX = evt.clientX;
          dragState.lastY = evt.clientY;
          if (Math.abs(evt.clientX - dragState.startX) > 4 || Math.abs(evt.clientY - dragState.startY) > 4) {
            dragState.moved = true;
          }

          var rect = viewport.getBoundingClientRect ? viewport.getBoundingClientRect() : { width: 800, height: 420 };
          var width = Math.max(240, rect.width || 800);
          var height = Math.max(180, rect.height || 420);

          if ($ctrl.zoom > 1.05) {
            $ctrl.panX += (dx / width) * (100 / $ctrl.zoom);
            $ctrl.panY += (dy / height) * (100 / $ctrl.zoom);
            $ctrl.tiltDeg = 0;
          } else {
            dragState.scrubAccum += dx;
            $ctrl.tiltDeg = Math.max(-3.5, Math.min(3.5, (dragState.scrubAccum / 90) * 3.5));
            if (Math.abs(dragState.scrubAccum) >= 75 && $ctrl.slides.length > 1) {
              var step = dragState.scrubAccum > 0 ? -1 : 1;
              dragState.scrubAccum = 0;
              $ctrl.tiltDeg = 0;
              $ctrl.activeAngleIndex = ($ctrl.activeAngleIndex + step + $ctrl.slides.length) % $ctrl.slides.length;
              $ctrl.activePart = null;
              updateActiveAngleMetadata();
            }
          }
          updateStageTransform();
          $scope.$applyAsync(angular.noop);
        }

        function onPointerUp(evt) {
          if (!dragState.active || (dragState.pointerId !== null && evt && evt.pointerId !== dragState.pointerId)) {
            return;
          }
          if (angular.isFunction(viewport.releasePointerCapture) && dragState.pointerId !== null) {
            try { viewport.releasePointerCapture(dragState.pointerId); } catch (e) { /* noop */ }
          }
          dragState.active = false;
          dragState.pointerId = null;
          $ctrl.isDragging = false;
          $ctrl.tiltDeg = 0;
          updateStageTransform();
          $scope.$applyAsync(angular.noop);
        }

        function onWheel(evt) {
          if (evt.cancelable) {
            evt.preventDefault();
          }
          var delta = evt.deltaY < 0 ? 0.22 : -0.22;
          $ctrl.zoomBy(delta);
          $scope.$applyAsync(angular.noop);
        }

        function onDblClick(evt) {
          if (evt.target && angular.isFunction(evt.target.closest) && evt.target.closest('.moto3d-hotspot-pin')) {
            return;
          }
          if ($ctrl.zoom > 1.2) {
            $ctrl.resetView();
          } else {
            var rect = viewport.getBoundingClientRect ? viewport.getBoundingClientRect() : { left: 0, top: 0, width: 800, height: 420 };
            var relX = ((evt.clientX - rect.left) / Math.max(1, rect.width)) * 100;
            var relY = ((evt.clientY - rect.top) / Math.max(1, rect.height)) * 100;
            $ctrl.zoom = 1.9;
            $ctrl.panX = (50 - relX) * 0.72;
            $ctrl.panY = (50 - relY) * 0.72;
            updateStageTransform();
          }
          $scope.$applyAsync(angular.noop);
        }

        function onKeyDown(evt) {
          if (evt.key === 'ArrowLeft') {
            evt.preventDefault();
            $ctrl.stepAngle(-1);
            $scope.$applyAsync(angular.noop);
          } else if (evt.key === 'ArrowRight') {
            evt.preventDefault();
            $ctrl.stepAngle(1);
            $scope.$applyAsync(angular.noop);
          } else if (evt.key === '+' || evt.key === '=') {
            evt.preventDefault();
            $ctrl.zoomBy(0.3);
            $scope.$applyAsync(angular.noop);
          } else if (evt.key === '-') {
            evt.preventDefault();
            $ctrl.zoomBy(-0.3);
            $scope.$applyAsync(angular.noop);
          } else if (evt.key === '0') {
            evt.preventDefault();
            $ctrl.resetView();
            $scope.$applyAsync(angular.noop);
          } else if (evt.key === 'Escape' && ($ctrl.activePart || $ctrl.zoom > 1.02)) {
            evt.preventDefault();
            evt.stopPropagation();
            $ctrl.resetView();
            $scope.$applyAsync(angular.noop);
          }
        }

        function onWindowEscape(evt) {
          if (evt.key === 'Escape' && ($ctrl.activePart || $ctrl.zoom > 1.02)) {
            evt.preventDefault();
            evt.stopPropagation();
            if (angular.isFunction(evt.stopImmediatePropagation)) {
              evt.stopImmediatePropagation();
            }
            $ctrl.resetView();
            $scope.$applyAsync(angular.noop);
          }
        }

        function onImageError(evt) {
          var img = evt && evt.target;
          if (!img || String(img.tagName || '').toUpperCase() !== 'IMG') {
            return;
          }
          var srcAttr = img.getAttribute ? (img.getAttribute('src') || '') : '';
          var changed = false;
          for (var i = 0; i < ($ctrl.slides || []).length; i++) {
            var sl = $ctrl.slides[i];
            if (sl.jpgUrl === srcAttr || sl.webpUrl === srcAttr) {
              if (sl.webpUrl) {
                sl.webpUrl = null;
                if (angular.isFunction(img.setAttribute)) {
                  img.setAttribute('src', sl.jpgUrl);
                }
                changed = true;
              } else if (sl.jpgUrl !== FALLBACK_JPG) {
                sl.jpgUrl = FALLBACK_JPG;
                if (angular.isFunction(img.setAttribute)) {
                  img.setAttribute('src', FALLBACK_JPG);
                }
                changed = true;
              }
            }
          }
          if (changed) {
            $scope.$applyAsync(angular.noop);
          }
        }

        viewport.addEventListener('pointerdown', onPointerDown);
        viewport.addEventListener('pointermove', onPointerMove);
        viewport.addEventListener('pointerup', onPointerUp);
        viewport.addEventListener('pointercancel', onPointerUp);
        viewport.addEventListener('wheel', onWheel, { passive: false });
        viewport.addEventListener('dblclick', onDblClick);
        viewport.addEventListener('keydown', onKeyDown);
        rootEl.addEventListener('error', onImageError, true);
        if ($window && angular.isFunction($window.addEventListener)) {
          $window.addEventListener('keydown', onWindowEscape, true);
        }

        cleanupListeners.push(function () {
          viewport.removeEventListener('pointerdown', onPointerDown);
          viewport.removeEventListener('pointermove', onPointerMove);
          viewport.removeEventListener('pointerup', onPointerUp);
          viewport.removeEventListener('pointercancel', onPointerUp);
          viewport.removeEventListener('wheel', onWheel);
          viewport.removeEventListener('dblclick', onDblClick);
          viewport.removeEventListener('keydown', onKeyDown);
          rootEl.removeEventListener('error', onImageError, true);
          if ($window && angular.isFunction($window.removeEventListener)) {
            $window.removeEventListener('keydown', onWindowEscape, true);
          }
        });
      } catch (e) {
        // Guarded for Node VM sandbox
      }
    }

    $ctrl.$onInit = function () {
      $ctrl.lightingTones = LIGHTING_TONES;
      $ctrl.componentList = COMPONENT_META;
      $ctrl.activeToneKey = 'daylight';
      $ctrl.activeToneClass = 'tone-daylight';
      $ctrl.activeAngleIndex = 0;
      $ctrl.activeAngleKey = 'front';
      $ctrl.activeSlideLabelKey = 'moto3d.camFront';
      $ctrl.activePart = null;
      $ctrl.showHotspots = true;
      $ctrl.autoTour = !isReducedMotionPreferred();
      $ctrl.isDragging = false;
      $ctrl.zoom = 1.0;
      $ctrl.panX = 0;
      $ctrl.panY = 0;
      $ctrl.tiltDeg = 0;
      $ctrl.visibleHotspots = [];
      $ctrl.slides = [];

      isInitialized = true;
      lastModelIdentity = computeModelIdentity();
      syncModelMetadata();
      updateStageTransform();
      syncAutoTourTimer();

      cleanupListeners.push($rootScope.$on('i18n:changed', function () {
        $scope.$applyAsync(angular.noop);
      }));
    };

    $ctrl.$postLink = function () {
      bindViewportEvents();
    };

    $ctrl.$onChanges = function () {
      if (!isInitialized) {
        return;
      }
      var nextIdentity = computeModelIdentity();
      syncModelMetadata();
      if (nextIdentity !== lastModelIdentity) {
        lastModelIdentity = nextIdentity;
        $ctrl.activePart = null;
        $ctrl.zoom = 1.0;
        $ctrl.panX = 0;
        $ctrl.panY = 0;
        $ctrl.tiltDeg = 0;
        updateStageTransform();
        syncAutoTourTimer();
      }
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      stopAutoTourTimer();
      for (var i = 0; i < cleanupListeners.length; i++) {
        cleanupListeners[i]();
      }
      cleanupListeners = [];
    };

    $ctrl.selectAngleIndex = function (idx) {
      if (!$ctrl.slides || !$ctrl.slides.length) {
        return;
      }
      $ctrl.autoTour = false;
      stopAutoTourTimer();
      $ctrl.activeAngleIndex = ((idx % $ctrl.slides.length) + $ctrl.slides.length) % $ctrl.slides.length;
      $ctrl.activePart = null;
      $ctrl.zoom = 1.0;
      $ctrl.panX = 0;
      $ctrl.panY = 0;
      $ctrl.tiltDeg = 0;
      updateActiveAngleMetadata();
      updateStageTransform();
    };

    $ctrl.stepAngle = function (delta) {
      $ctrl.selectAngleIndex($ctrl.activeAngleIndex + delta);
    };

    $ctrl.toggleAutoTour = function () {
      $ctrl.autoTour = !$ctrl.autoTour;
      if ($ctrl.autoTour) {
        $ctrl.activePart = null;
        $ctrl.zoom = 1.0;
        $ctrl.panX = 0;
        $ctrl.panY = 0;
        updateStageTransform();
      }
      syncAutoTourTimer();
    };

    $ctrl.toggleHotspots = function () {
      $ctrl.showHotspots = !$ctrl.showHotspots;
    };

    $ctrl.selectTone = function (toneKey) {
      for (var i = 0; i < LIGHTING_TONES.length; i++) {
        if (LIGHTING_TONES[i].key === toneKey) {
          $ctrl.activeToneKey = toneKey;
          $ctrl.activeToneClass = LIGHTING_TONES[i].cssClass;
          return;
        }
      }
    };

    $ctrl.zoomBy = function (delta) {
      $ctrl.autoTour = false;
      stopAutoTourTimer();
      var next = Math.max(1.0, Math.min(2.6, $ctrl.zoom + delta));
      $ctrl.zoom = Math.round(next * 100) / 100;
      if ($ctrl.zoom <= 1.02) {
        $ctrl.zoom = 1.0;
        $ctrl.panX = 0;
        $ctrl.panY = 0;
      }
      updateStageTransform();
    };

    $ctrl.resetView = function () {
      $ctrl.activePart = null;
      $ctrl.zoom = 1.0;
      $ctrl.panX = 0;
      $ctrl.panY = 0;
      $ctrl.tiltDeg = 0;
      updateStageTransform();
    };

    $ctrl.clearSelectedPart = function () {
      $ctrl.resetView();
    };

    $ctrl.onHotspotClick = function ($event, partId) {
      if ($event && angular.isFunction($event.stopPropagation)) {
        $event.stopPropagation();
      }
      $ctrl.focusComponent(partId, true);
    };

    $ctrl.focusComponent = function (partId, stayOnCurrentAngleIfPresent) {
      var found = null;
      for (var i = 0; i < COMPONENT_META.length; i++) {
        if (COMPONENT_META[i].id === partId) {
          found = COMPONENT_META[i];
          break;
        }
      }
      if (!found) {
        return;
      }

      $ctrl.autoTour = false;
      stopAutoTourTimer();
      $ctrl.showHotspots = true;

      var targetAngleKey = (stayOnCurrentAngleIfPresent && found.pins && found.pins[$ctrl.activeAngleKey])
        ? $ctrl.activeAngleKey
        : found.bestAngle;

      for (var j = 0; j < $ctrl.slides.length; j++) {
        if ($ctrl.slides[j].key === targetAngleKey) {
          $ctrl.activeAngleIndex = j;
          break;
        }
      }

      updateActiveAngleMetadata();
      $ctrl.activePart = found;

      var coord = (found.pins && (found.pins[$ctrl.activeAngleKey] || found.pins[found.bestAngle])) || { x: 50, y: 50 };
      $ctrl.zoom = found.zoom || 1.8;
      $ctrl.panX = (50 - coord.x) * 0.78;
      $ctrl.panY = (50 - coord.y) * 0.78;
      $ctrl.tiltDeg = 0;
      updateStageTransform();
    };
  }
})();

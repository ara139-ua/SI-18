using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SI18.Api.Infraestructura.Migraciones
{
    /// <inheritdoc />
    public partial class NucleoComun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "centros",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_centros", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clubes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clubes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deportes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deportes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tarifas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_instalacion = table.Column<string>(type: "text", nullable: false),
                    tipo_tarifa = table.Column<string>(type: "text", nullable: false),
                    precio_hora = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tarifas", x => x.id);
                    table.CheckConstraint("ck_tarifas_precio_hora_positivo", "precio_hora > 0");
                    table.CheckConstraint("ck_tarifas_tipo_instalacion_pista", "tipo_instalacion IN ('pista_tenis', 'pista_padel')");
                    table.CheckConstraint("ck_tarifas_tipo_instalacion_valores", "tipo_instalacion IN ('pista_tenis', 'pista_padel', 'pabellon', 'sala', 'piscina')");
                    table.CheckConstraint("ck_tarifas_tipo_tarifa_valores", "tipo_tarifa IN ('empadronado', 'no_empadronado', 'club')");
                });

            migrationBuilder.CreateTable(
                name: "temporadas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_temporadas", x => x.id);
                    table.CheckConstraint("ck_temporadas_fechas", "fecha_fin > fecha_inicio");
                });

            migrationBuilder.CreateTable(
                name: "instalaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    centro_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    reservable_por_franjas = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instalaciones", x => x.id);
                    table.CheckConstraint("ck_instalaciones_reservable_por_franjas", "reservable_por_franjas = (tipo IN ('pista_tenis', 'pista_padel'))");
                    table.CheckConstraint("ck_instalaciones_tipo_valores", "tipo IN ('pista_tenis', 'pista_padel', 'pabellon', 'sala', 'piscina')");
                    table.ForeignKey(
                        name: "fk_instalaciones_centros_centro_id",
                        column: x => x.centro_id,
                        principalTable: "centros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    hash_contrasena = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rol = table.Column<string>(type: "text", nullable: false),
                    empadronado = table.Column<bool>(type: "boolean", nullable: false),
                    club_id = table.Column<int>(type: "integer", nullable: true),
                    centro_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.CheckConstraint("ck_usuarios_centro_si_personal", "(rol = 'personal_centro') = (centro_id IS NOT NULL)");
                    table.CheckConstraint("ck_usuarios_club_si_representante", "(rol = 'representante_club') = (club_id IS NOT NULL)");
                    table.CheckConstraint("ck_usuarios_rol_valores", "rol IN ('ciudadano', 'representante_club', 'personal_centro', 'tecnico_concejalia')");
                    table.ForeignKey(
                        name: "fk_usuarios_centros_centro_id",
                        column: x => x.centro_id,
                        principalTable: "centros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuarios_clubes_club_id",
                        column: x => x.club_id,
                        principalTable: "clubes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cursos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    deporte_id = table.Column<int>(type: "integer", nullable: false),
                    instalacion_id = table.Column<int>(type: "integer", nullable: false),
                    trimestre = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    dias_semana = table.Column<short[]>(type: "smallint[]", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    plazas_maximas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cursos", x => x.id);
                    table.CheckConstraint("ck_cursos_fechas", "fecha_fin > fecha_inicio");
                    table.CheckConstraint("ck_cursos_horas", "hora_fin > hora_inicio");
                    table.CheckConstraint("ck_cursos_plazas_maximas", "plazas_maximas > 0");
                    table.ForeignKey(
                        name: "fk_cursos_deportes_deporte_id",
                        column: x => x.deporte_id,
                        principalTable: "deportes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cursos_instalaciones_instalacion_id",
                        column: x => x.instalacion_id,
                        principalTable: "instalaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "instalacion_deportes",
                columns: table => new
                {
                    instalacion_id = table.Column<int>(type: "integer", nullable: false),
                    deporte_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instalacion_deportes", x => new { x.instalacion_id, x.deporte_id });
                    table.ForeignKey(
                        name: "fk_instalacion_deportes_deportes_deporte_id",
                        column: x => x.deporte_id,
                        principalTable: "deportes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_instalacion_deportes_instalaciones_instalacion_id",
                        column: x => x.instalacion_id,
                        principalTable: "instalaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "solicitudes_franjas_fijas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    club_id = table.Column<int>(type: "integer", nullable: false),
                    instalacion_id = table.Column<int>(type: "integer", nullable: false),
                    temporada_id = table.Column<int>(type: "integer", nullable: false),
                    dias_semana = table.Column<short[]>(type: "smallint[]", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    creada_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resuelta_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitudes_franjas_fijas", x => x.id);
                    table.CheckConstraint("ck_solicitudes_franjas_fijas_dias_semana", "cardinality(dias_semana) > 0 AND dias_semana <@ ARRAY[1,2,3,4,5,6,7]::smallint[]");
                    table.CheckConstraint("ck_solicitudes_franjas_fijas_estado_valores", "estado IN ('pendiente', 'aprobada', 'rechazada')");
                    table.CheckConstraint("ck_solicitudes_franjas_fijas_horas", "hora_fin > hora_inicio");
                    table.CheckConstraint("ck_solicitudes_franjas_fijas_resuelta", "(estado <> 'pendiente') = (resuelta_en_utc IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_solicitudes_franjas_fijas_clubes_club_id",
                        column: x => x.club_id,
                        principalTable: "clubes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_solicitudes_franjas_fijas_instalaciones_instalacion_id",
                        column: x => x.instalacion_id,
                        principalTable: "instalaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_solicitudes_franjas_fijas_temporadas_temporada_id",
                        column: x => x.temporada_id,
                        principalTable: "temporadas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cierres_mantenimiento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    instalacion_id = table.Column<int>(type: "integer", nullable: false),
                    inicio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fin_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: false),
                    registrado_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cierres_mantenimiento", x => x.id);
                    table.CheckConstraint("ck_cierres_mantenimiento_intervalo", "fin_utc > inicio_utc");
                    table.ForeignKey(
                        name: "fk_cierres_mantenimiento_instalaciones_instalacion_id",
                        column: x => x.instalacion_id,
                        principalTable: "instalaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cierres_mantenimiento_usuarios_registrado_por_id",
                        column: x => x.registrado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    texto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    fecha_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    leida = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notificaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_notificaciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    instalacion_id = table.Column<int>(type: "integer", nullable: false),
                    inicio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fin_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo_tarifa = table.Column<string>(type: "text", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    creada_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    codigo_acceso = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    cancelada_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_cancelacion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservas", x => x.id);
                    table.CheckConstraint("ck_reservas_cancelada_en", "(estado = 'cancelada') = (cancelada_en_utc IS NOT NULL)");
                    table.CheckConstraint("ck_reservas_codigo_acceso", "(estado IN ('pendiente_pago', 'caducada')) = (codigo_acceso IS NULL)");
                    table.CheckConstraint("ck_reservas_duracion", "fin_utc = inicio_utc + interval '1 hour'");
                    table.CheckConstraint("ck_reservas_estado_valores", "estado IN ('pendiente_pago', 'confirmada', 'caducada', 'cancelada')");
                    table.CheckConstraint("ck_reservas_importe_positivo", "importe > 0");
                    table.CheckConstraint("ck_reservas_motivo_cancelacion", "(estado = 'cancelada') = (motivo_cancelacion IS NOT NULL)");
                    table.CheckConstraint("ck_reservas_motivo_cancelacion_valores", "motivo_cancelacion IN ('usuario', 'cierre_mantenimiento')");
                    table.CheckConstraint("ck_reservas_tipo_tarifa_valores", "tipo_tarifa IN ('empadronado', 'no_empadronado', 'club')");
                    table.ForeignKey(
                        name: "fk_reservas_instalaciones_instalacion_id",
                        column: x => x.instalacion_id,
                        principalTable: "instalaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inscripciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    curso_id = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    llegada_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inscripciones", x => x.id);
                    table.CheckConstraint("ck_inscripciones_estado_valores", "estado IN ('confirmada', 'lista_espera', 'baja')");
                    table.ForeignKey(
                        name: "fk_inscripciones_cursos_curso_id",
                        column: x => x.curso_id,
                        principalTable: "cursos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inscripciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reserva_id = table.Column<long>(type: "bigint", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    creado_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resuelto_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagos", x => x.id);
                    table.CheckConstraint("ck_pagos_estado_valores", "estado IN ('pendiente', 'completado', 'fallido')");
                    table.CheckConstraint("ck_pagos_resuelto_en", "(estado <> 'pendiente') = (resuelto_en_utc IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_pagos_reservas_reserva_id",
                        column: x => x.reserva_id,
                        principalTable: "reservas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "validaciones_acceso",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reserva_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo_leido = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    aceptada = table.Column<bool>(type: "boolean", nullable: false),
                    motivo_rechazo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    fecha_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    personal_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_validaciones_acceso", x => x.id);
                    table.CheckConstraint("ck_validaciones_acceso_motivo_rechazo", "(NOT aceptada) = (motivo_rechazo IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_validaciones_acceso_reservas_reserva_id",
                        column: x => x.reserva_id,
                        principalTable: "reservas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_validaciones_acceso_usuarios_personal_id",
                        column: x => x.personal_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devoluciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pago_id = table.Column<long>(type: "bigint", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: false),
                    fecha_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devoluciones", x => x.id);
                    table.CheckConstraint("ck_devoluciones_motivo_valores", "motivo IN ('cancelacion_usuario', 'cierre_mantenimiento')");
                    table.ForeignKey(
                        name: "fk_devoluciones_pagos_pago_id",
                        column: x => x.pago_id,
                        principalTable: "pagos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_centros_nombre",
                table: "centros",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cierres_mantenimiento_instalacion_id",
                table: "cierres_mantenimiento",
                column: "instalacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_cierres_mantenimiento_registrado_por_id",
                table: "cierres_mantenimiento",
                column: "registrado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_clubes_nombre",
                table: "clubes",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cursos_deporte_id",
                table: "cursos",
                column: "deporte_id");

            migrationBuilder.CreateIndex(
                name: "ix_cursos_instalacion_id",
                table: "cursos",
                column: "instalacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_deportes_nombre",
                table: "deportes",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devoluciones_pago_id",
                table: "devoluciones",
                column: "pago_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inscripciones_curso_id",
                table: "inscripciones",
                column: "curso_id");

            migrationBuilder.CreateIndex(
                name: "ix_inscripciones_usuario_id",
                table: "inscripciones",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_instalacion_deportes_deporte_id",
                table: "instalacion_deportes",
                column: "deporte_id");

            migrationBuilder.CreateIndex(
                name: "ix_instalaciones_centro_id_nombre",
                table: "instalaciones",
                columns: new[] { "centro_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notificaciones_usuario_id_fecha_utc",
                table: "notificaciones",
                columns: new[] { "usuario_id", "fecha_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_pagos_reserva_id",
                table: "pagos",
                column: "reserva_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reservas_codigo_acceso",
                table: "reservas",
                column: "codigo_acceso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reservas_estado_creada_en_utc",
                table: "reservas",
                columns: new[] { "estado", "creada_en_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_reservas_usuario_id_inicio_utc",
                table: "reservas",
                columns: new[] { "usuario_id", "inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_reservas_activa",
                table: "reservas",
                columns: new[] { "instalacion_id", "inicio_utc" },
                unique: true,
                filter: "estado IN ('pendiente_pago', 'confirmada')");

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_franjas_fijas_club_id",
                table: "solicitudes_franjas_fijas",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_franjas_fijas_instalacion_id",
                table: "solicitudes_franjas_fijas",
                column: "instalacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_franjas_fijas_temporada_id",
                table: "solicitudes_franjas_fijas",
                column: "temporada_id");

            migrationBuilder.CreateIndex(
                name: "ix_tarifas_tipo_instalacion_tipo_tarifa",
                table: "tarifas",
                columns: new[] { "tipo_instalacion", "tipo_tarifa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_temporadas_nombre",
                table: "temporadas",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_centro_id",
                table: "usuarios",
                column: "centro_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_club_id",
                table: "usuarios",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_correo",
                table: "usuarios",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_validaciones_acceso_personal_id",
                table: "validaciones_acceso",
                column: "personal_id");

            migrationBuilder.CreateIndex(
                name: "ix_validaciones_acceso_reserva_id",
                table: "validaciones_acceso",
                column: "reserva_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cierres_mantenimiento");

            migrationBuilder.DropTable(
                name: "devoluciones");

            migrationBuilder.DropTable(
                name: "inscripciones");

            migrationBuilder.DropTable(
                name: "instalacion_deportes");

            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropTable(
                name: "solicitudes_franjas_fijas");

            migrationBuilder.DropTable(
                name: "tarifas");

            migrationBuilder.DropTable(
                name: "validaciones_acceso");

            migrationBuilder.DropTable(
                name: "pagos");

            migrationBuilder.DropTable(
                name: "cursos");

            migrationBuilder.DropTable(
                name: "temporadas");

            migrationBuilder.DropTable(
                name: "reservas");

            migrationBuilder.DropTable(
                name: "deportes");

            migrationBuilder.DropTable(
                name: "instalaciones");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "centros");

            migrationBuilder.DropTable(
                name: "clubes");
        }
    }
}

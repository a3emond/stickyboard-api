--------------------------------------------------
-- Worker Jobs (generic async queue)
--------------------------------------------------
CREATE TABLE IF NOT EXISTS worker_jobs (
  id           bigserial PRIMARY KEY,
  kind         worker_job_kind   NOT NULL,
  payload      jsonb             NOT NULL,
  status       worker_job_status NOT NULL DEFAULT 'queued',
  priority     int               NOT NULL DEFAULT 5,
  attempts     int               NOT NULL DEFAULT 0,
  created_at   timestamptz       NOT NULL DEFAULT now(),
  updated_at   timestamptz       NOT NULL DEFAULT now(),
  available_at timestamptz       NOT NULL DEFAULT now(),
  last_error   text
);

CREATE INDEX IF NOT EXISTS ix_jobs_ready
  ON worker_jobs(status, priority, available_at);


--------------------------------------------------
-- Worker Job Attempts
--------------------------------------------------
CREATE TABLE IF NOT EXISTS worker_job_attempts (
  id          bigserial PRIMARY KEY,
  job_id      bigint NOT NULL REFERENCES worker_jobs(id) ON DELETE CASCADE,
  started_at  timestamptz NOT NULL,
  finished_at timestamptz,
  error       text
);

-- Validate character profession state after manual P0 test.
-- Replace @character_id and @job_id before running.

SET @character_id = 0;   -- TODO: test character Id
SET @job_id = 28;        -- TODO: job learned (28=Paysan, 2=Bûcheron, 41=Chasseur, 36=Pêcheur)

-- 1) Character has the job row
SELECT OwnerId, Job, Experience
FROM characters_jobs
WHERE OwnerId = @character_id AND Job = @job_id;

-- 2) No duplicate rows for same job
SELECT Job, COUNT(*) AS row_count
FROM characters_jobs
WHERE OwnerId = @character_id
GROUP BY Job
HAVING row_count > 1;

-- 3) All jobs for character
SELECT cj.Job, j.Name, cj.Experience
FROM characters_jobs cj
LEFT JOIN jobs j ON j.Id = cj.Job
WHERE cj.OwnerId = @character_id
ORDER BY cj.Job;

-- 4) Computed level from experiences table (compare with server ExperienceManager)
SELECT cj.Job, cj.Experience,
       (SELECT MAX(e.Level) FROM experiences e WHERE e.JobExp <= cj.Experience) AS computed_level
FROM characters_jobs cj
WHERE cj.OwnerId = @character_id AND cj.Job = @job_id;

-- 5) XP threshold sanity (level 1 floor)
SELECT MIN(JobExp) AS level_1_job_exp_floor FROM experiences WHERE JobExp > 0;

-- 6) Orphan job rows
SELECT cj.*
FROM characters_jobs cj
LEFT JOIN characters c ON c.Id = cj.OwnerId
WHERE c.Id IS NULL;

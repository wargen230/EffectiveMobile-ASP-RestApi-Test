from locust import HttpUser, task, between
import os
import logging
import random


class WebsiteTestUser(HttpUser):
    wait_time = between(0.5, 3.0)
    host = "http://localhost:5001"

    def on_start(self):
        self.file_path = "/root/TestAPI/TestAPI.PythonTests/TestData/data.txt"
        self.locations = ["ru", "ru/svrd", "ru/chelobl"]
        self.file_exists = os.path.exists(self.file_path)

        if not self.file_exists:
            logging.warning(f"File {self.file_path} not found! Upload tasks will be skipped.")

    @task(1)
    def hello_world(self):
        with self.client.get("/",
                             name="Hello World",
                             catch_response=True) as response:
            if response.status_code == 200:
                response.success()
            else:
                response.failure(f"Failed with status {response.status_code}")

    @task(2)
    def upload_file(self):
        if not self.file_exists:
            return

        try:
            with open(self.file_path, "rb") as file:
                files = {"file": ("data.txt", file, "text/plain")}
                with self.client.post("/api/upload",
                                      files=files,
                                      name="File Upload",
                                      catch_response=True) as response:
                    if response.status_code in [200, 201, 202]:
                        response.success()
                    else:
                        response.failure(f"Upload failed: {response.status_code}")

        except Exception:
            logging.error("File upload error", exc_info=True)

    @task(3)
    def get_platform_by_location(self):
        location = random.choice(self.locations)
        with self.client.get(f"/api/search?location={location}",
                             name="Search by Location",
                             catch_response=True) as response:
            if response.status_code == 200:
                response.success()
            else:
                response.failure(f"Search failed for {location}: {response.status_code}")

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProtocoloZero.Camera.Tests
{
    public class OrbitCameraControllerTests : InputTestFixture
    {
        private GameObject target;
        private GameObject cameraObject;
        private OrbitCameraController orbit;
        private Mouse mouse;
        private Keyboard keyboard;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            target = new GameObject("Player");
            target.AddComponent<CharacterController>();
            cameraObject = new GameObject("Camera");
            cameraObject.AddComponent<UnityEngine.Camera>();
            orbit = cameraObject.AddComponent<OrbitCameraController>();
            Field("target").SetValue(orbit, target.transform);
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            Tick(0f);
        }

        [TearDown]
        public override void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(target);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            base.TearDown();
        }

        private static FieldInfo Field(string name) =>
            typeof(OrbitCameraController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

        private void Tick(float dt) =>
            typeof(OrbitCameraController).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(orbit, new object[] { dt });

        [Test]
        public void StartsBehindPlayerAndTracksWithoutChangingFacing()
        {
            target.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Field("initialized").SetValue(orbit, false);
            Tick(0f);
            Assert.That(cameraObject.transform.position.x, Is.LessThan(-2f));
            Assert.That(cameraObject.transform.position.y, Is.GreaterThan(target.transform.position.y));
            target.transform.position += Vector3.right * 3f;
            Tick(0.02f);
            Assert.That(cameraObject.transform.position.x, Is.GreaterThan(-3f));
            Assert.That(target.transform.eulerAngles.y, Is.EqualTo(90f).Within(0.01f));
        }

        [Test]
        public void MousePixelsOrbitAndPitchIsClamped()
        {
            Set(mouse.delta, new Vector2(100f, 10000f));
            typeof(OrbitCameraController).GetMethod("ApplyMouseDelta", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(orbit, new object[] { mouse.delta.ReadValue() });
            Tick(0.02f);
            Assert.That(cameraObject.transform.eulerAngles.y, Is.GreaterThan(0f));
            Assert.That(Vector3.Dot(cameraObject.transform.forward, Vector3.up), Is.GreaterThan(0f));
            Assert.That(Mathf.DeltaAngle(0f, cameraObject.transform.eulerAngles.x), Is.EqualTo(-35f).Within(0.01f));
            Assert.That(target.transform.eulerAngles.y, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void SelfAndTriggersAreIgnoredButWallRetractsAndRecovers()
        {
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                trigger.transform.position = new Vector3(0f, 1.6f, -1f);
                trigger.GetComponent<Collider>().isTrigger = true;
                Tick(0f);
                float clear = Vector3.Distance(cameraObject.transform.position, target.transform.position);
                wall.transform.position = new Vector3(0f, 1.6f, -2f);
                wall.transform.localScale = new Vector3(5f, 5f, 0.2f);
                Physics.SyncTransforms();
                Tick(0.02f);
                float blocked = Vector3.Distance(cameraObject.transform.position, target.transform.position);
                Assert.That(blocked, Is.LessThan(clear - 0.5f));
                Object.DestroyImmediate(wall);
                Tick(0.02f);
                Assert.That(Vector3.Distance(cameraObject.transform.position, target.transform.position), Is.GreaterThan(blocked));
            }
            finally
            {
                Object.DestroyImmediate(trigger);
                if (wall != null) Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void MissingTargetAndInputDoNotThrow()
        {
            Field("target").SetValue(orbit, null);
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(keyboard);
            Assert.DoesNotThrow(() => Tick(0.02f));
        }

        [Test]
        public void SolidEnclosingPivotRetractsRatherThanPassingThrough()
        {
            var enclosure = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                enclosure.transform.position = new Vector3(0f, 1.5f, 0f);
                enclosure.transform.localScale = Vector3.one;
                Physics.SyncTransforms();
                Tick(0.02f);
                Assert.That(Vector3.Distance(cameraObject.transform.position,
                    target.transform.position + Vector3.up * 1.5f), Is.EqualTo(0f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(enclosure);
            }
        }
    }
}

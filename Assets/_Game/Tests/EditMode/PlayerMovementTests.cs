using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProtocoloZero.Player.Tests
{
    public class PlayerMovementTests : InputTestFixture
    {
        private GameObject actor;
        private PlayerMovement movement;
        private Keyboard keyboard;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            actor = new GameObject("Player");
            actor.AddComponent<CharacterController>();
            movement = actor.AddComponent<PlayerMovement>();
        }

        [TearDown]
        public override void TearDown()
        {
            Object.DestroyImmediate(actor);
            base.TearDown();
        }

        private void Tick()
        {
            typeof(PlayerMovement).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(movement, new object[] { 1f });
        }

        [Test]
        public void WorldForwardUsesPositiveZ()
        {
            Press(keyboard.wKey);
            Tick();
            Assert.That(actor.transform.position.z, Is.GreaterThan(0f));
            Assert.That(actor.transform.position.x, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void DiagonalSpeedDoesNotExceedCardinalSpeed()
        {
            Press(keyboard.wKey);
            Tick();
            float cardinal = new Vector2(actor.transform.position.x, actor.transform.position.z).magnitude;

            // Compare independent first steps; accumulated gravity alters collision sliding.
            Object.DestroyImmediate(actor);
            actor = new GameObject("Diagonal player");
            actor.AddComponent<CharacterController>();
            movement = actor.AddComponent<PlayerMovement>();
            Press(keyboard.dKey);
            Tick();
            float diagonal = new Vector2(actor.transform.position.x, actor.transform.position.z).magnitude;

            Assert.That(diagonal, Is.EqualTo(cardinal).Within(0.02f));
        }

        [Test]
        public void ShiftSprintIsFasterThanWalk()
        {
            Press(keyboard.wKey);
            Tick();
            float walk = actor.transform.position.z;

            actor.transform.position = Vector3.zero;
            Press(keyboard.leftShiftKey);
            Tick();
            Assert.That(actor.transform.position.z, Is.GreaterThan(walk));
        }

        [Test]
        public void NoKeyboardDoesNotMoveHorizontally()
        {
            InputSystem.RemoveDevice(keyboard);
            Tick();
            Assert.That(actor.transform.position.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(actor.transform.position.z, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void ReferenceProjectsTiltedAxesOntoGround()
        {
            var reference = new GameObject("Movement reference");
            try
            {
                reference.transform.rotation = Quaternion.Euler(30f, 90f, 0f);
                typeof(PlayerMovement).GetField("movementReference", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(movement, reference.transform);
                Press(keyboard.wKey);
                Tick();
                Assert.That(actor.transform.position.x, Is.GreaterThan(0f));
                Assert.That(actor.transform.position.z, Is.EqualTo(0f).Within(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(reference);
            }
        }
    }
}
